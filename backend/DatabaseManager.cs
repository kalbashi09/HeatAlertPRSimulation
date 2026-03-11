using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeatAlert 
{
    public class DatabaseManager 
    {
        private string connString = "server=localhost;database=HeatIndicator;uid=root;pwd=naturemoonsea;";

        public async Task SaveHeatLog(AlertResult result)
        {
            try 
            {
                using var connection = new MySqlConnection(connString);
                await connection.OpenAsync();
                Console.WriteLine("--- DB Debug: Connection Opened ---"); // Debug line

                string query = @"INSERT INTO heat_logs (barangay, heat_index, latitude, longitude, created_at) 
                                VALUES (@brgy, @heat, @lat, @lng, NOW())";
                                
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@brgy", result.BarangayName ?? "Unknown");
                cmd.Parameters.AddWithValue("@heat", result.HeatIndex);
                cmd.Parameters.AddWithValue("@lat", result.Lat);
                cmd.Parameters.AddWithValue("@lng", result.Lng);
                
                int rows = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"--- DB Debug: Rows Affected: {rows} ---"); // Debug line
            }
            catch (Exception ex) // Catch EVERYTHING
            {
                Console.WriteLine($"[CRITICAL DB ERROR]: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        //
        // Store Alert Data to Database for frontend GET!
        public async Task<List<AlertResult>> GetHistory(int limit = 20)
        {
            var logs = new List<AlertResult>();
            try 
            {
                using var connection = new MySqlConnection(connString);
                await connection.OpenAsync();
                
                string query = "SELECT barangay, heat_index, latitude, longitude FROM heat_logs ORDER BY created_at DESC LIMIT @limit";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@limit", limit);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    logs.Add(new AlertResult {
                        BarangayName = reader.GetString(0),
                        HeatIndex = reader.GetInt32(1),
                        Lat = reader.GetDouble(2),
                        Lng = reader.GetDouble(3),
                        RelativeLocation = "Historical Record"
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine($"[DB ERROR] {ex.Message}"); }
            return logs;
        }

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


        // Saves a history log of the heat reading
        

        // Additional database methods can be added here

    }
}