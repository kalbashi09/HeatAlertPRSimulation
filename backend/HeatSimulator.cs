using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;

namespace HeatAlert
{
    public class AlertResult
    {
        public string BarangayName { get; set; } = string.Empty;
        public string RelativeLocation { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int HeatIndex { get; set; }
    }

    public class HeatSimulator
    {

        private string GetRelativeDirection(double centerLat, double centerLng, double spikeLat, double spikeLng)
        {
            // Simple coordinate comparison for cardinal directions
            string latDir = spikeLat > centerLat ? "North" : "South";
            string lngDir = spikeLng > centerLng ? "East" : "West";

            // Check if it's right at the center (within a small threshold)
            double threshold = 0.001; 
            if (Math.Abs(spikeLat - centerLat) < threshold && Math.Abs(spikeLng - centerLng) < threshold)
                return "right at the center";

            // Combine them (e.g., "North-West")
            return $"{latDir}-{lngDir}";
        }

        public string GetDangerLevel(int heatIndex)
        {
            if (heatIndex >= 49) return "🚨 EXTREME DANGER";
            if (heatIndex >= 42) return "🔥 DANGER";
            if (heatIndex >= 39) return "⚠️ EXTREME CAUTION"; 
            if (heatIndex >= 30) return "✅ NORMAL";          
            return "❄️ COOL";                                 // Anything below 30
        }

        public AlertResult GenerateAlert(string jsonPath)
        {
            // 1. Read and Parse the JSON
            string jsonContent = File.ReadAllText(jsonPath);
            // The ! tells C# "I guarantee this won't be null, shut up warnings"
            JObject data = JObject.Parse(jsonContent)!; 
            JArray features = (JArray)data["features"]!;

            // 2. Pick a random Barangay (Feature)
            Random rng = new Random();
            // Fixes CS8602 for the properties
            var selectedFeature = features[rng.Next(features.Count)];
            string barangay = selectedFeature["properties"]?["NAME_3"]?.ToString() ?? "Unknown";

            // Fixes CS8602/CS8604 for the coordinates
            var geometry = selectedFeature["geometry"];
            var coords = geometry?["coordinates"]?[0];

            if (coords == null) 
                {
                    // Instead of returning null, return a 'Safe' dummy to avoid crashes
                    return new AlertResult { BarangayName = "Error", HeatIndex = 0 }; 
                } // Safety check to stop warnings

            // 4. Extract and flatten the coordinates into a list we can measure
            // This tells LINQ to look at each pair [lng, lat] and pull out the 0 index for Lng and 1 for Lat
            var coordList = coords.Select(c => new { 
                Lng = (double)(c?[0] ?? 0), 
                Lat = (double)(c?[1] ?? 0) 
            });

            double minLng = coordList.Min(c => c.Lng);
            double maxLng = coordList.Max(c => c.Lng);
            double minLat = coordList.Min(c => c.Lat);
            double maxLat = coordList.Max(c => c.Lat);

            double centerLat = coordList.Average(c => c.Lat);
            double centerLng = coordList.Average(c => c.Lng);

            // 5. Generate random Lat/Lng within that specific box
            double randomLat = rng.NextDouble() * (maxLat - minLat) + minLat;
            double randomLng = rng.NextDouble() * (maxLng - minLng) + minLng;
            int randomHeat = rng.Next(27, 51); // Generate a dangerous temp

            string direction = GetRelativeDirection(centerLat, centerLng, randomLat, randomLng);

            return new AlertResult
            {
                BarangayName = barangay,
                Lat = randomLat,
                Lng = randomLng,
                HeatIndex = randomHeat,
                RelativeLocation = $"{direction} of {barangay}"
            };
        }
    }
}