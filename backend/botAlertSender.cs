using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HeatAlert
{
    public class BotAlertSender
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseManager _db;
        private static readonly Dictionary<long, string> _pendingSimulations = new();

        public BotAlertSender(string token, DatabaseManager db)
        {
            _botClient = new TelegramBotClient(token);
            _db = db;
        }

        public void StartBot()
        {
            var receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions);
            Console.WriteLine("🤖 Bot is now listening for subscribers...");
        }

        // --- NEW: CENTRALIZED METHOD (SINGLE SOURCE OF TRUTH) ---
        // This handles updating the map, formatting the text, and broadcasting.
        public async Task ProcessAndBroadcastAlert(AlertResult result)
        {
            // 1. Update the Web Map Bridge
            GlobalData.LatestAlert = result;

            // 2. Determine Danger Level
            var simulator = new HeatSimulator();
            string level = simulator.GetDangerLevel(result.HeatIndex);

            // 3. One Format for Everything
            string alertMsg = $"{level}\n" +
                              $"🌡️ Temp: {result.HeatIndex}°C\n" +
                              $"📍 Location: {result.BarangayName}\n" +
                              $"🌐 Coord: {result.Lat:F4}, {result.Lng:F4}";

            // 4. Send to all subscribers
            var subs = await _db.GetAllSubscriberIds();
            await BroadcastAlert(alertMsg, subs);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Message?.Location != null)
            {
                await ProcessManualSensorPing(bot, update.Message, ct);
                return;
            }

            if (update.Message is not { Text: not null } message) return;

            long chatId = message.Chat.Id;
            string username = message.From?.Username ?? "UnknownUser";
            string text = message.Text.ToLower();

            string[] simCommands = { "/exdanger", "/danger", "/caution", "/normal", "/cool" };
            if (simCommands.Contains(text))
            {
                _pendingSimulations[chatId] = text;
                var keyboard = new ReplyKeyboardMarkup(new[] {
                    new KeyboardButton("📡 Confirm Sensor Location") { RequestLocation = true }
                }) { ResizeKeyboard = true, OneTimeKeyboard = true };

                await bot.SendMessage(chatId, $"🛠️ **Simulation: {text.ToUpper()}**\nTap the button below to send GPS.", 
                    replyMarkup: keyboard, cancellationToken: ct);
                return;
            }

            if (text == "/subscribeservice")
            {
                await _db.SaveSubscriber(chatId, username);
                await bot.SendMessage(chatId, "✅ Subscribed!", cancellationToken: ct);
            }
            else if (text == "/unsubscribeservice")
            {
                await _db.RemoveSubscriber(chatId);
                await bot.SendMessage(chatId, "👋 Unsubscribed.");
            }
        }

        private async Task ProcessManualSensorPing(ITelegramBotClient bot, Message message, CancellationToken ct)
        {
            long chatId = message.Chat.Id;
            if (!_pendingSimulations.TryGetValue(chatId, out var command)) command = "/danger";

            int simTemp = command switch {
                "/exdanger" => 49,
                "/danger"   => 42,
                "/caution"  => 39,
                "/normal"   => 30,
                _           => 24 
            };

            // Use the simulator to package the data (Reverse Geocoding included)
            var simulator = new HeatSimulator();
            var result = simulator.CreateManualAlert(
                message.Location!.Latitude, 
                message.Location.Longitude, 
                simTemp, 
                "../sharedresource/talisaycitycebu.json"
            );

            // CALL THE CENTRALIZED METHOD
            await ProcessAndBroadcastAlert(result);

            await bot.SendMessage(chatId, $"✅ Signal Sent to Map and Subscribers.", 
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
            _pendingSimulations.Remove(chatId);
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Bot Error: {ex.Message}");
            return Task.CompletedTask;
        }

        public async Task BroadcastAlert(string alertMsg, List<long> subscriberIds)
        {
            int sentCount = 0;
            foreach (var id in subscriberIds)
            {
                try {
                    await _botClient.SendMessage(chatId: id, text: alertMsg);
                    sentCount++;
                } catch { /* Ignore blocked bots */ }
            }
            Console.WriteLine($"📢 Broadcast: {sentCount} users notified.");
        }
    }
}