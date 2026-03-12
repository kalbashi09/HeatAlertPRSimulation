Information In this read me are for instructional

Check commands.txt to run the server

DATABASE TEMPLATE: {

CREATE DATABASE HeatIndicator;
use HeatIndicator;

CREATE TABLE subscribers (
chat_id BIGINT PRIMARY KEY,
username VARCHAR(100),
subscribed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

SELECT \* FROM subscribers;

truncate table subscribers;

CREATE DATABASE HeatIndicator;
use HeatIndicator;

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

SELECT \* FROM heat_logs;
}
