# 🛠️ HEAT ALERT SYSTEM (HEALERTSYS) | CHANGELOG

All notable changes to this project will be documented in this file.
This project adheres to a custom iterative development cycle.

---

## [1.2.0] - 2026-03-14

### 🔵 ADVANCED / INTEGRATED (C# Migration Complete)

### 🔒 Security & Connectivity

- **VS Code Dev Tunnels:** Integrated persistent public URLs (`.devtunnels.ms`) for remote frontend collaboration.
- **"The Bouncer" Auth:** Implemented `X-API-KEY` validation and Anti-Phishing bypass headers (`X-Tunnel-Skip-AntiPhishing-Page`).
- **CORS Policy:** Enabled Cross-Origin Resource Sharing for seamless browser-to-API communication.

### 🗺️ Geo-Spatial Refinement

- **Centroid + Bounding Box:** Replaced vertex-picking with Bounding Box center logic to ensure markers land in the "heart" of Barangays.
- **Safe-Zone Jitter:** Added a 20% random offset to prevent marker stacking on identical coordinates.
- **Ray-Casting Gatekeeper:** Maintained `IsPointInPolygon` as a final validation layer for coordinate accuracy.

### 🗄️ Database & Optimization

- **Anomalous Filtering:** Configured DB-level filters to ignore normal temperatures (29°C–38°C), archiving only danger/cool anomalies.
- **Auto-Rotation:** Implemented `CleanupOldLogs()` using LIMIT/OFFSET to maintain a 100-record cap, optimizing performance for i7-4700MQ hardware.
- **Manual Simulation:** Added a `/api/log-heat` POST endpoint to allow manual alert injections from the web.

---

## [1.1.0] - 2026-02-07

### 🟢 FUNCTIONAL / STABLE

### 🧠 Simulation Engine

- **Geo-Spatial Randomization:** First implementation of Long/Lat/Temp simulation within Talisay parameters.
- **Barangay Mapping:** Initial integration of `talisaycitycebu.json` for Min/Max coordinate bounds.
- **CPU Optimization:** Configured smart throttling for `while` loops to prevent local hardware lag.

### 🤖 Telegram Bot & UI

- **Rich-Text Notifications:** Added severity labels (Caution to Extreme Danger) and human-readable locations (e.g., "North-West of Bulacao").
- **Subscriber Analytics:** Added SQL-driven tracker to monitor active notification users.

---

## [1.0.0] - 2026-02-02

### 🚀 INITIAL RELEASE (XAMPP/C# Foundation)

### 🏗️ System Architecture

- **Dependency Setup:** Integrated `MySqlConnector`, `Telegram.Bot`, and `Newtonsoft.Json`.
- **Decoupled Logic:** Separated `BotAlertSender.cs` (Messaging) and `DatabaseManager.cs` (Persistence).

### ✅ Core Features

- **Bot Handshake:** Full C# Engine to Telegram API communication.
- **Subscription Service:** Implemented `/subscriberservice` and `/unsubscriberservice` commands.
- **Broadcast Engine:** Initial logic to fetch SQL chat IDs and broadcast high-heat triggers.

---

## [Planned / Next Steps]

- **Leaflet.js Mapping:** Visualizing the Centroid data on a real-world Talisay map.
- **WebSocket/SignalR:** Real-time push notifications to eliminate manual dashboard refreshes.
- **Data Analytics:** Building a heat-map trend visualizer based on the `alert_logs` history.
