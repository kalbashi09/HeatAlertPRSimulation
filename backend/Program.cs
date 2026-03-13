using Microsoft.Extensions.DependencyInjection;
using HeatAlert;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

string connString = builder.Configuration.GetConnectionString("DefaultConnection")!;
string botToken = builder.Configuration["BotSettings:TelegramToken"]!;

// 1. ROBUST PATH CHECKING
string baseDir = AppDomain.CurrentDomain.BaseDirectory;
// We try the most likely path first (3 levels up from bin/Debug/netX.0)
string jsonPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "sharedresource", "talisaycitycebu.json"));

// FALLBACK: If 3 levels fails, try 4 levels (sometimes VS structure varies)
if (!File.Exists(jsonPath)) {
    jsonPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "sharedresource", "talisaycitycebu.json"));
}

string cachedJson = "";
try {
    cachedJson = File.ReadAllText(jsonPath);
    Console.WriteLine($"✅ GeoJSON loaded from: {jsonPath}");
} catch (Exception ex) {
    // If this hits, the simulation will definitely fail.
    Console.WriteLine($"❌ CRITICAL PATH ERROR: {ex.Message}");
}

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var db = new DatabaseManager(connString);
var bot = new BotAlertSender(botToken, db, cachedJson);

builder.Services.AddSingleton(db);
builder.Services.AddSingleton(bot);

var app = builder.Build();

app.UseCors("AllowAll");
app.RegisterAlertEndpoints(db);

// 2. START SIMULATION
_ = Task.Run(async () => {
    bot.StartBot();
    
    // Pass the data to the simulator once
    var simulator = new HeatSimulator(cachedJson); 
    var rng = new Random();

    while (true)
    {
        try {
            int simTemp = rng.Next(25, 52); 

            // Pass the cached string
            var randomPoint = GetRandomTalisayPoint(cachedJson);
            
            // This method in HeatSimulator.cs must NOT take a 'path' anymore
            var result = simulator.CreateManualAlert(randomPoint.lat, randomPoint.lng, simTemp); 
            
            GlobalData.LatestAlert = result; 
            await db.SaveHeatLog(result); 
            
            Console.WriteLine($"[LOG] {result.BarangayName} | {result.HeatIndex}°C | {DateTime.Now:hh:mm:ss tt}");

            if (result.HeatIndex >= 39 || result.HeatIndex < 29)
            {
                await bot.ProcessAndBroadcastAlert(result);
            }
        }
        catch (Exception ex) { Console.WriteLine($"Simulation Loop Error: {ex.Message}"); }
        
        await Task.Delay(30000); 
    }
});

app.Run();

// --- HELPERS ---
(double lat, double lng) GetRandomTalisayPoint(string jsonContent) {
    if (string.IsNullOrEmpty(jsonContent)) return (10.2447, 123.8480);
    
    try {
        var data = JObject.Parse(jsonContent);
        var features = (JArray)data["features"]!;
        var rng = new Random(); // Create one instance to use
        
        // 1. Pick a random Barangay
        var randomBarangay = features[rng.Next(features.Count)];
        var geometry = randomBarangay["geometry"];
        string type = geometry?["type"]?.ToString() ?? "";

        // 2. Get the list of all corners (vertices) for that Barangay
        JToken? allCoords = type switch {
            "Polygon" => geometry?["coordinates"]?[0],
            "MultiPolygon" => geometry?["coordinates"]?[0]?[0],
            _ => null
        };

        if (allCoords == null || !allCoords.HasValues) return (10.2447, 123.8480);

        // 3. INNOVATION: Pick a RANDOM corner from the array instead of always [0]
        int totalPoints = allCoords.Count();
        int randomPointIndex = rng.Next(totalPoints);
        var selectedPoint = allCoords[randomPointIndex];

        // GeoJSON is [lng, lat], so we return [1, 0]
        return ((double)selectedPoint[1], (double)selectedPoint[0]); 
    } catch {
        // Fallback to City Hall if the JSON parsing fails
        return (10.2447, 123.8480);
    }
}

public static class GlobalData {
    public static AlertResult? LatestAlert { get; set; }
}