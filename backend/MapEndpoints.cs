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
                return GlobalData.LatestAlert != null 
                    ? Results.Ok(GlobalData.LatestAlert) 
                    : Results.NotFound("No data yet.");
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