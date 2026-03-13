using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HeatAlert 
{
    public class DatabaseManager 
    {
        private readonly string _connString;
        public DatabaseManager(string connString) 
        {
            _connString = connString;
        }
        public async Task SaveHeatLog(AlertResult result)
        {
            // --- THE FILTER ---
            // Ignore data IF it is greater than or equal to 29 AND less than or equal to 38.
            if (result.HeatIndex >= 29 && result.HeatIndex <= 38) 
            {
                Console.WriteLine($"--- DB Skip: {result.HeatIndex}°C is in the 'Normal' range (29-38). ---");
                return; 
            }

            try 
            {
                using var connection = new MySqlConnection(_connString);
                await connection.OpenAsync();

                // This only runs if the temp is < 29 OR > 38
                string query = @"INSERT INTO heat_logs (barangay, heat_index, latitude, longitude, created_at) 
                                VALUES (@brgy, @heat, @lat, @lng, NOW())";
                                
                using var cmd = new MySqlCommand(query, connection);
                
                cmd.Parameters.AddWithValue("@brgy", result.BarangayName ?? "Unknown");
                cmd.Parameters.AddWithValue("@heat", result.HeatIndex);
                cmd.Parameters.AddWithValue("@lat", result.Lat);
                cmd.Parameters.AddWithValue("@lng", result.Lng);
                
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"--- DB Saved: {result.BarangayName} recorded at {result.HeatIndex}°C ---");

                _ = CleanupOldLogs();
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"[CRITICAL DB ERROR]: {ex.Message}");
            }
        }

        private async Task CleanupOldLogs() // This keeps only the latest 100 logs in the database to prevent bloat
        {
            try
            {
                using var connection = new MySqlConnection(_connString);
                await connection.OpenAsync();

                // Subquery: Find the ID of the 300th record (ordered newest to oldest)
                // Anything with an ID smaller than that is "Old" and gets deleted.
                string query = @"
                    DELETE FROM heat_logs 
                    WHERE id < (
                        SELECT id FROM (
                            SELECT id FROM heat_logs 
                            ORDER BY created_at DESC 
                            LIMIT 1 OFFSET 100 
                        ) AS tmp
                    )";

                using var cmd = new MySqlCommand(query, connection);
                int deletedRows = await cmd.ExecuteNonQueryAsync();
                
                if (deletedRows > 0)
                {
                    Console.WriteLine($"--- DB Cleanup: Removed {deletedRows} old logs ---");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB CLEANUP ERROR]: {ex.Message}");
            }
        }
        
        // Store Alert Data to Database for frontend GET!
        public async Task<List<AlertResult>> GetHistory(int limit = 100, int offset = 0)
        {
            var logs = new List<AlertResult>();
            try 
            {
                using var connection = new MySqlConnection(_connString);
                await connection.OpenAsync();
                
                // Modified query to include OFFSET for pagination
                string query = @"SELECT barangay, heat_index, latitude, longitude, created_at 
                                FROM heat_logs 
                                ORDER BY created_at DESC 
                                LIMIT @limit OFFSET @offset";
                
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@limit", limit);
                cmd.Parameters.AddWithValue("@offset", offset); // Add this parameter

                using var reader = await cmd.ExecuteReaderAsync();
       
                while (await reader.ReadAsync())
                {
                    logs.Add(new AlertResult {
                        BarangayName = reader.GetString(0),
                        HeatIndex = reader.GetInt32(1),
                        Lat = reader.GetDouble(2),
                        Lng = reader.GetDouble(3),
                        CreatedAt = reader.GetDateTime(4), // Map the timestamp here
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
            using var connection = new MySqlConnection(_connString);
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
            using var connection = new MySqlConnection(_connString);
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
            using var connection = new MySqlConnection(_connString);
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