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
}
