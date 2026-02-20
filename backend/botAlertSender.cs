using Telegram.Bot;
using Telegram.Bot.Polling; // Needed for StartReceiving
using Telegram.Bot.Types;

namespace HeatAlert
{
    public class BotAlertSender
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseManager _db; // Link to your SQL manager

        public BotAlertSender(string token, DatabaseManager db)
        {
            _botClient = new TelegramBotClient(token);
            _db = db;
        }

        // --- THE LISTENER ---
        public void StartBot()
        {
            var receiverOptions = new ReceiverOptions { AllowedUpdates = { } }; // Receive all update types
            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions);
            Console.WriteLine("🤖 Bot is now listening for subscribers...");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Message is not { Text: not null } message) return;

            long chatId = message.Chat.Id;
            string username = message.From?.Username ?? "UnknownUser";

            if (message.Text.ToLower() == "/subscribeservice")
            {
                await _db.SaveSubscriber(chatId, username);
                
                // Updated to use the more modern SendMessage
                await bot.SendMessage(
                    chatId: chatId, 
                    text: "✅ You WILL NOW RECEIVE HEAT SIGNATURE UPDATES within Talisay Heat Alerts!",
                    cancellationToken: ct
                );
            }

            // Inside HandleUpdateAsync in botAlertSender.cs
            if (message.Text.ToLower() == "/subscribeservice")
            {
                await _db.SaveSubscriber(chatId, username);
                await bot.SendMessage(chatId, "✅ Subscribed!");
            }
            else if (message.Text.ToLower() == "/unsubscribeservice") // NEW COMMAND
            {
                await _db.RemoveSubscriber(chatId);
                await bot.SendMessage(chatId, "👋 You have been unsubscribed. You will no longer receive heat alerts.");
            }

        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Bot Error: {ex.Message}");
            return Task.CompletedTask;
        }

        // --- THE BROADCASTER ---
        // Instead of sending to ONE hardcoded ID, send to the whole list from DB
        // --- THE BROADCASTER ---
// Refactored to accept a pre-formatted string instead of raw doubles
        public async Task BroadcastAlert(string alertMsg, List<long> subscriberIds)
        {
            // Use a counter for better console logging
            int sentCount = 0;

            foreach (var id in subscriberIds)
            {
                try 
                {
                    // Note: Telegram.Bot v21+ uses SendMessage instead of SendTextMessageAsync
                    await _botClient.SendMessage(
                        chatId: id, 
                        text: alertMsg
                    );
                    sentCount++;
                } 
                catch (Exception ex)
                {
                    // Useful for debugging if a user blocked the bot
                    Console.WriteLine($"⚠️ Could not send to {id}: {ex.Message}");
                }
            }
            Console.WriteLine($"📢 Broadcast complete. Sent to {sentCount} subscribers.");
        }
    }
}