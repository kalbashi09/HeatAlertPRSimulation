# 📍 HEAT ALERT SYSTEM (HEALERTSYS)

Information in this README are for instructional use.

Check commands.txt to run the server.

---

## [DATABASE TEMPLATE]

CREATE DATABASE HeatIndicator;
USE HeatIndicator;

CREATE TABLE subscribers (
chat_id BIGINT PRIMARY KEY,
username VARCHAR(100),
subscribed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- For Maintenance:
-- SELECT \* FROM subscribers;
-- TRUNCATE TABLE subscribers;

CREATE TABLE IF NOT EXISTS heat_logs (
id INT AUTO_INCREMENT PRIMARY KEY,
barangay VARCHAR(100) NOT NULL,
heat_index INT NOT NULL,
latitude DOUBLE(10, 6) NOT NULL,
longitude DOUBLE(10, 6) NOT NULL,
created_at DATETIME DEFAULT CURRENT_TIMESTAMP,

    -- Indexing for performance (great for your future history charts!)
    INDEX idx_barangay (barangay),
    INDEX idx_created (created_at)

);

-- For Maintenance:
-- SELECT \* FROM heat_logs;

---

[** FOR FRONT END **]
-- USE this API END POINT SCRIPT TO GAIN ACCESS WITH the data running in the API --

---

const apiURL = "https://3h48gqgv-5000.asse.devtunnels.ms/api/heat-history";

async function getHeatHistory() {
try {
const response = await fetch(apiURL, {
method: "GET",
headers: {
// 1. Your Custom Security Key
"X-API-KEY": "Talisay_Secret_2026",

                // 2. The "Bypass" for VS Code Tunnels
                "X-Tunnel-Skip-AntiPhishing-Page": "true",

                "Accept": "application/json"
            }
        });

        if (!response.ok) {
            throw new Error(`Bouncer says no! Status: ${response.status}`);
        }

        const data = await response.json();
        console.log("Heat Data Received:", data);
        return data;

    } catch (error) {
        console.error("Connection failed:", error);
    }

}

---

## [PROJECT IMPLEMENTATION DETAILS]

- Geo-Spatial: Implemented Centroid + Bounding Box logic to ensure points land inside Barangays.
- Validation: Ray-Casting (IsPointInPolygon) added to prevent "Outside Talisay" errors.
- Security: Integrated X-API-KEY "Bouncer" middleware and Tunnel Anti-Phishing bypass.
- Performance: DB prunes logs to the last 100 entries to save resources on i7-4700MQ.
- Filtering: System ignores normal range (29°C-38°C) to prioritize anomaly alerts.

---

[NEXT STEPS]

- Integrate Leaflet.js for real-time visual mapping.
- Implement WebSocket/SignalR for live dashboard updates.
