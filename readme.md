# 📍 HEAT ALERT SYSTEM (HEALERTSYS)

A lightweight heat monitoring and alert system designed to detect and log **dangerous heat index anomalies** within barangays.

Built with performance in mind for low-resource hardware while still supporting future visualization and live updates.

---

# ⚠️ Notice

These instructions are **for instructional and development purposes**.

To run the server, check:

`commands.txt`

---

# 🛠️ Database Setup

### SQL Template

```sql
CREATE DATABASE HeatIndicator;
USE HeatIndicator;

CREATE TABLE subscribers (
    chat_id BIGINT PRIMARY KEY,
    username VARCHAR(100),
    subscribed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Maintenance Commands
-- SELECT * FROM subscribers;
-- TRUNCATE TABLE subscribers;

CREATE TABLE IF NOT EXISTS heat_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    barangay VARCHAR(100) NOT NULL,
    heat_index INT NOT NULL,
    latitude DOUBLE(10, 6) NOT NULL,
    longitude DOUBLE(10, 6) NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,

    -- Indexing for performance
    INDEX idx_barangay (barangay),
    INDEX idx_created (created_at)
);

-- Maintenance Commands
-- SELECT * FROM heat_logs;
```

---

# 🌐 Frontend API Access

Use the following JavaScript to access the heat history API.

---

```javascript
const apiURL = "https://3h48gqgv-5000.asse.devtunnels.ms/api/name-of-api";

async function getHeatHistory() {
  try {
    const response = await fetch(apiURL, {
      method: "GET",
      headers: {
        // Custom security key
        "X-API-KEY": "Talisay_Secret_2026",

        // VS Code Dev Tunnel bypass
        "X-Tunnel-Skip-Anti-Phishing-Page": "true",

        Accept: "application/json",
      },
    });

    if (!response.ok) {
      throw new Error(`Bouncer says no! Status: ${response.status}`);
    }

    const data = await response.json();
    console.log("🔥 Heat Data Received:", data);
    return data;
  } catch (error) {
    console.error("🚫 Connection failed:", error);
  }
}
```

---

# 🏗️ Implementation Details

### Geo-Spatial Logic

Uses **Centroid + Bounding Box calculations** to ensure location points fall within barangay boundaries.

### Validation

Implements **Ray-Casting (`IsPointInPolygon`)** to prevent points from appearing outside the Talisay area.

### Security

Includes a **custom API authentication layer** using:

`X-API-KEY`

Also supports **VS Code Tunnel Anti-Phishing bypass headers**.

### Performance

The database automatically **prunes logs to the latest 100 entries** to maintain performance on lower-resource hardware.

### Smart Filtering

The system **ignores normal heat ranges**:

`29°C – 38°C`

This ensures alerts only trigger for **abnormal heat spikes**.

---

# 🚀 Roadmap

Planned improvements for future versions:

- [ ] Integrate **Maplibre.js** for interactive map visualization
- [ ] Add **WebSocket / SignalR** for real-time dashboard updates
- [ ] Historical heat trend charts

---
