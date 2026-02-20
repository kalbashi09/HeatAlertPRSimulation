using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.DependencyInjection;
using HeatAlert;

var builder = WebApplication.CreateBuilder(args);

// 1. Register CORS - Must be before builder.Build()
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// 2. Activate CORS - Must be before app.Run()
app.UseCors("AllowAll");

// --- YOUR ORIGINAL OBJECTS ---
var db = new DatabaseManager();
var bot = new BotAlertSender("8439622862:AAGCRTIItpNNK3UUNT8pUMRwd5WlywyRh1M", db); 
var simulator = new HeatSimulator(); 
AlertResult latestAlert = null; // Bridge for the API

// 3. API ENDPOINT for your Front-end Developer
app.MapGet("/api/current-alert", () => {
    return latestAlert != null ? Results.Ok(latestAlert) : Results.NotFound("No data yet.");
});

// 4. YOUR ORIGINAL LOOP (Wrapped in a Task to prevent freezing the API)
_ = Task.Run(async () => {
    bot.StartBot();
    Console.WriteLine("🚀 Monitoring system active. Simulation loop starting...");

    while (true)
    {
        // Update the bridge variable so the API can see it
        latestAlert = simulator.GenerateAlert("../sharedresource/talisaycitycebu.json");
        string level = simulator.GetDangerLevel(latestAlert.HeatIndex);

        if (latestAlert.HeatIndex >= 39)
        {
            string message = $"{level}\n" +
                             $"🌡️ Temp: {latestAlert.HeatIndex}°C\n" +
                             $"📍 Location: {latestAlert.RelativeLocation}\n" +
                             $"🌐 Coord: {latestAlert.Lat:F4}, {latestAlert.Lng:F4}";

            var subscribers = await db.GetAllSubscriberIds();
            await bot.BroadcastAlert(message, subscribers);
            Console.WriteLine($"[BROADCAST] Sent {level} to {subscribers.Count} users for {latestAlert.BarangayName}.");
        }
        else if (latestAlert.HeatIndex < 30)
        {
            string message = $"{level}\n" +
                             $"🌡️ Temp: {latestAlert.HeatIndex}°C\n" +
                             $"📍 Location: {latestAlert.RelativeLocation}\n" +
                             $"🌐 Coord: {latestAlert.Lat:F4}, {latestAlert.Lng:F4}";

            var subscribers = await db.GetAllSubscriberIds();
            await bot.BroadcastAlert(message, subscribers);
            Console.WriteLine($"[BROADCAST] Sent {level} to {subscribers.Count} users for {latestAlert.BarangayName}.");
        }
        else
        {
            Console.WriteLine($"[STABLE] {latestAlert.BarangayName}: {latestAlert.HeatIndex}°C ({level}). No alert sent.");
        }

        await Task.Delay(30000); 
    }
});

app.Run(); // Starts the server (Kestrel) on http://localhost:5000 (usually)