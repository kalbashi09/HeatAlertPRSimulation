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
app.UseRouting();

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
        var rng = new Random(); 
        
        // 1. Pick a random Barangay
        var randomBarangay = features[rng.Next(features.Count)];
        var geometry = randomBarangay["geometry"];
        string type = geometry?["type"]?.ToString() ?? "";

        // 2. Get the list of all corners (vertices)
        JToken? allCoords = type switch {
            "Polygon" => geometry?["coordinates"]?[0],
            "MultiPolygon" => geometry?["coordinates"]?[0]?[0],
            _ => null
        };

        if (allCoords == null || !allCoords.HasValues) return (10.2447, 123.8480);

        // --- NEW INNOVATION: BOUNDING BOX CENTER + SAFE JITTER ---
        var pointsList = allCoords.Children().ToList();

        // Find the "Box" that contains the whole Barangay
        double minLng = pointsList.Min(p => (double)p[0]);
        double maxLng = pointsList.Max(p => (double)p[0]);
        double minLat = pointsList.Min(p => (double)p[1]);
        double maxLat = pointsList.Max(p => (double)p[1]);

        // Find the center of that box
        double centerLat = (minLat + maxLat) / 2;
        double centerLng = (minLng + maxLng) / 2;

        // Add a 20% "Safe Jitter" so points aren't identical but stay near the center
        // This keeps markers away from the jagged borders/Cebu City lines
        double latRange = (maxLat - minLat) * 0.2;
        double lngRange = (maxLng - minLng) * 0.2;

        double finalLat = centerLat + ((rng.NextDouble() - 0.5) * latRange);
        double finalLng = centerLng + ((rng.NextDouble() - 0.5) * lngRange);

        return (finalLat, finalLng); 
    } catch {
        // Fallback to City Hall
        return (10.2447, 123.8480);
    }
}

public static class GlobalData {
    public static AlertResult? LatestAlert { get; set; }
}