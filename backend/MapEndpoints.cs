using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HeatAlert
{
    public static class MapEndpoints
    {
        public static void RegisterAlertEndpoints(this IEndpointRouteBuilder app, DatabaseManager db)
        {
            // GET: Fetch the current heat data
            app.MapGet("/api/current-alert", () => {
                var alert = GlobalData.LatestAlert;

                if (alert == null) return Results.NotFound("No data yet.");

                // Logic: If the live alert is in that "Boring" range, 
                // maybe we tell the frontend there is no 'Active' Alert.
                if (alert.HeatIndex >= 29 && alert.HeatIndex <= 38)
                {
                    return Results.Ok(new { 
                        Status = "Stable", 
                        Message = "Temperatures are within normal range.",
                        LastReading = alert.HeatIndex,
                        Barangay = alert.BarangayName
                    });
                }

                // Otherwise, return the full Danger alert
                return Results.Ok(alert);
            });

            // GET: Fetch the history of heat logs from the database
            app.MapGet("/api/heat-history", async (DatabaseManager db, int? limit) => 
            {
                int finalLimit = limit ?? 100;
                try 
                {
                    var history = await db.GetHistory(finalLimit);
                    
                    if (!history.Any()) return Results.NotFound("Database is empty.");

                    // We "Select" and transform the data into a more 'friendly' JSON shape
                    var friendlyHistory = history.Select(h => new {
                        h.BarangayName,
                        h.HeatIndex,
                        h.Lat,
                        h.Lng,
                        Date = h.CreatedAt.ToString("MMM dd, yyyy"), // "Mar 12, 2026"
                        Time = h.CreatedAt.ToString("hh:mm tt"),    // "06:55 PM"
                        RawTimestamp = h.CreatedAt                   // Keep this for sorting!
                    });

                    return Results.Ok(friendlyHistory);
                }
                catch (Exception ex) 
                {
                    return Results.Problem($"Database Error: {ex.Message}");
                }
            });

            // POST: Manually add a subscriber (e.g., from a Web Form)
            app.MapPost("/api/subscribe", async (SubscriberRequest request) => {
                if (request.ChatId == 0 || string.IsNullOrEmpty(request.Username))
                {
                    return Results.BadRequest("Invalid subscriber data.");
                }

                try 
                {
                    await db.SaveSubscriber(request.ChatId, request.Username);
                    return Results.Ok(new { message = "Successfully subscribed via API!" });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Database Error: {ex.Message}");
                }
            });

            // Inside RegisterAlertEndpoints in MapEndpoints.cs
            app.MapPost("/api/log-heat", async (AlertResult data, DatabaseManager db, BotAlertSender bot) => 
            {
                try 
                {
                    // 1. Save to Database
                    await db.SaveHeatLog(data);
                    
                    // 2. USE YOUR CENTRALIZED METHOD!
                    // This updates the map AND sends the Telegram messages automatically.
                    await bot.ProcessAndBroadcastAlert(data);

                    return Results.Ok(new { message = "Log saved and Broadcast sent!", data });
                }
                catch (Exception ex) 
                {
                    return Results.Problem($"API Error: {ex.Message}");
                }
            });
        }
    }

    // A simple "DTO" (Data Transfer Object) to handle the incoming JSON
    public record SubscriberRequest(long ChatId, string Username);
}