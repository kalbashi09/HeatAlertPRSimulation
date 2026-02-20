using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeatAlert 
{
    public class DatabaseManager 
    {
        private string connString = "server=localhost;database=HeatIndicator;uid=root;pwd=shinju0728;";

        // Saves User When They Subscribe
        public async Task SaveSubscriber(long chatId, string username) 
        {
            using var connection = new MySqlConnection(connString);
            await connection.OpenAsync();
            string query = "INSERT IGNORE INTO subscribers (chat_id, username) VALUES (@id, @user)";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", chatId);
            cmd.Parameters.AddWithValue("@user", username ?? "Unknown");
            await cmd.ExecuteNonQueryAsync();
        }

        // Removes User When They Unsubscribe
        public async Task RemoveSubscriber(long chatId) 
        {
            using var connection = new MySqlConnection(connString);
            await connection.OpenAsync();

            // Use a parameterized query to safely remove the user
            string query = "DELETE FROM subscribers WHERE chat_id = @id";
            
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", chatId);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0) {
                Console.WriteLine($"User {chatId} has been removed from the database.");
            } else {
                Console.WriteLine($"User {chatId} was not found in the database.");
            }
        }

        // Fetches All Subscriber IDs
        public async Task<List<long>> GetAllSubscriberIds()
        {
            var ids = new List<long>();
            using var connection = new MySqlConnection(connString);
            await connection.OpenAsync();
            string query = "SELECT chat_id FROM subscribers";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt64(0));
            }
            return ids;
        }

        // Additional database methods can be added here
    }
}