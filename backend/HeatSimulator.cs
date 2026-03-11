using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System; // Added for Math and Console

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
        public string GetDangerLevel(int heatIndex)
        {
            if (heatIndex >= 49) return "🚨 EXTREME DANGER";
            if (heatIndex >= 42) return "🔥 DANGER";
            if (heatIndex >= 39) return "⚠️ EXTREME CAUTION"; 
            if (heatIndex >= 29) return "✅ NORMAL";           
            return "❄️ COOL";
        }

        public string IdentifyBarangay(double lat, double lng, string jsonPath)
        {
            // FIX CS0103: Declare these BEFORE the try block so they exist everywhere
            string closestBarangay = "Talisay City";
            double minDistance = double.MaxValue;

            try
            {
                if (!File.Exists(jsonPath)) return "File Not Found";

                string jsonContent = File.ReadAllText(jsonPath);
                JObject data = JObject.Parse(jsonContent);
                JArray? features = data["features"] as JArray;

                if (features == null) return "Invalid GeoJSON";

                foreach (var feature in features)
                {
                    string name = feature["properties"]?["NAME_3"]?.ToString() ?? "Unknown";
                    var coords = feature["geometry"]?["coordinates"]?[0];

                    if (coords != null && coords.HasValues)
                    {
                        // 1. Check if it's INSIDE (Perfect Match)
                        if (IsPointInPolygon(lat, lng, coords)) return name;

                        // 2. FALLBACK: Calculate distance to avoid "Outside Known Barangay"
                        // FIX CS8602/CS8604: Added null-checks for the coordinate access
                        var firstPoint = coords[0];
                        if (firstPoint != null && firstPoint.Count() >= 2)
                        {
                            double firstLng = (double)firstPoint[0]!;
                            double firstLat = (double)firstPoint[1]!;
                            
                            // Simple Pythagorean distance
                            double dist = Math.Sqrt(Math.Pow(lat - firstLat, 2) + Math.Pow(lng - firstLng, 2));

                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                closestBarangay = name;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"[GEO-ERROR] {ex.Message}"); 
            }

            // If we are within ~500 meters of a boundary, just give them that Barangay
            return (minDistance < 0.005) ? closestBarangay : "Talisay (Outside)";
        }

        private bool IsPointInPolygon(double lat, double lng, JToken polygon)
        {
            bool isInside = false;
            var points = polygon.Children().ToList(); 
            int j = points.Count - 1;

            for (int i = 0; i < points.Count; i++)
            {
                double piLng = (double)points[i][0]!;
                double piLat = (double)points[i][1]!;
                double pjLng = (double)points[j][0]!;
                double pjLat = (double)points[j][1]!;

                if ((((piLat <= lat) && (lat < pjLat)) || ((pjLat <= lat) && (lat < piLat))) &&
                    (lng < (pjLng - piLng) * (lat - piLat) / (pjLat - piLat) + piLng))
                {
                    isInside = !isInside;
                }
                j = i;
            }
            return isInside;
        }

        public AlertResult CreateManualAlert(double lat, double lng, int heatIndex, string jsonPath)
        {
            string barangay = IdentifyBarangay(lat, lng, jsonPath);

            return new AlertResult
            {
                BarangayName = barangay,
                Lat = lat,
                Lng = lng,
                HeatIndex = heatIndex,
                RelativeLocation = "Live Mobile Sensor"
            };
        }
    }
}