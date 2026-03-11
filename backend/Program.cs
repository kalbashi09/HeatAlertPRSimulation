using Microsoft.Extensions.DependencyInjection;
using HeatAlert;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

// Pull values from appsettings.json
string connString = builder.Configuration.GetConnectionString("DefaultConnection")!;
string botToken = builder.Configuration["BotSettings:TelegramToken"]!;

// 1. SETUP SERVICES FIRST
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Create your objects
var db = new DatabaseManager(connString);
var bot = new BotAlertSender(botToken, db);

// Register them BEFORE builder.Build()
builder.Services.AddSingleton(db);
builder.Services.AddSingleton(bot);

var app = builder.Build();

// 2. CONFIGURE MIDDLEWARE
app.UseCors("AllowAll");
app.RegisterAlertEndpoints(db); // Pass db directly if needed, or let DI handle it

// 3. START BACKGROUND SIMULATION
_ = Task.Run(async () => {
    bot.StartBot();
    Console.WriteLine("🚀 Monitoring system active...");
    
    var simulator = new HeatSimulator(); 
    var rng = new Random();

    while (true)
    {
        try {
            // MOVE rng.Next INSIDE the loop so every 30s is a different temp!
            int simTemp = rng.Next(25, 52); 

            var randomPoint = GetRandomTalisayPoint("../sharedresource/talisaycitycebu.json");
            
            var result = simulator.CreateManualAlert(
                randomPoint.lat, 
                randomPoint.lng, 
                simTemp, 
                "../sharedresource/talisaycitycebu.json"
            );
            
            GlobalData.LatestAlert = result; 
            await db.SaveHeatLog(result); // This writes it to MySQL
            Console.WriteLine($"[LOG] Location: {result.BarangayName} | Temp: {result.HeatIndex}°C");

            // Only broadcast if it's dangerous or unusually cool
            if (result.HeatIndex >= 39 || result.HeatIndex < 30)
            {
                await bot.ProcessAndBroadcastAlert(result);
            }
        }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        
        await Task.Delay(30000); // 30 seconds
    }
});

app.Run();

// --- HELPERS ---
(double lat, double lng) GetRandomTalisayPoint(string path) {
    var json = File.ReadAllText(path);
    var data = JObject.Parse(json);
    var features = (JArray)data["features"]!;
    
    var randomBarangay = features[new Random().Next(features.Count)];
    var geometry = randomBarangay["geometry"];
    string type = geometry?["type"]?.ToString() ?? "";

    // This handles both simple Polygons and complex MultiPolygons common in Cebu maps
    JToken? coord = type switch {
        "Polygon" => geometry?["coordinates"]?[0]?[0],
        "MultiPolygon" => geometry?["coordinates"]?[0]?[0]?[0],
        _ => null
    };

    if (coord == null) return (10.2447, 123.8480); // Default to Talisay City Hall if it fails
    return ((double)coord[1], (double)coord[0]); 
}

public static class GlobalData {
    public static AlertResult? LatestAlert { get; set; }
}