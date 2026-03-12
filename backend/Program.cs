using Microsoft.Extensions.DependencyInjection;
using HeatAlert;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

// Pull values from appsettings.json
string connString = builder.Configuration.GetConnectionString("DefaultConnection")!;
string botToken = builder.Configuration["BotSettings:TelegramToken"]!;

// 1. Define the dynamic path at the top of your Program.cs (after builder is created)
string baseDir = AppDomain.CurrentDomain.BaseDirectory;
// This goes from bin/Debug -> bin -> backend -> HeatAlertPRSimulation -> sharedresource
string jsonPath = Path.GetFullPath(Path.Combine(baseDir , "..", "..", "..", "..", "sharedresource", "talisaycitycebu.json"));

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
    var simulator = new HeatSimulator(); 
    var rng = new Random();

    while (true)
    {
        try {
            int simTemp = rng.Next(25, 52); 

            // Use jsonPath instead of "../sharedresource/..."
            var randomPoint = GetRandomTalisayPoint(jsonPath);
            
            var result = simulator.CreateManualAlert(
                randomPoint.lat, 
                randomPoint.lng, 
                simTemp, 
                jsonPath // CHANGE THIS TOO
            ); 
            
            GlobalData.LatestAlert = result; 
            await db.SaveHeatLog(result); 
            
            if (result.HeatIndex >= 39 || result.HeatIndex < 29)
            {
                await bot.ProcessAndBroadcastAlert(result);
            }
        }
        catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        
        await Task.Delay(30000); 
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
