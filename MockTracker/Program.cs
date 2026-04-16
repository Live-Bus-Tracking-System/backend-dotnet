using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

// ─────────────────────────────────────────────────────────────────────────────
//  BusTracker Mock GPS Tracker
//  Fully standalone — no project references whatsoever.
//  Simulates a bus driving along a configurable list of GPS waypoints, sending
//  signed pings to the live BusTracker API every PING_INTERVAL_SECONDS.
// ─────────────────────────────────────────────────────────────────────────────

var config = new TrackerConfig
{
    // ── CHANGE THESE ─────────────────────────────────────────────────────────
    ApiBaseUrl          = "http://localhost:5279",    // HTTP profile from launchSettings.json
    TrackerId           = "TRACKER_001",              // Matches Vehicles.TrackerId in DB
    MasterKey           = "V2zDzZx8lQaZbPi8mDRg7WqOhD7mjknETFo0Ql",     // ← paste your Tracking:MasterKey here
    PingIntervalSeconds = 3,
    // ─────────────────────────────────────────────────────────────────────────

    // These match the 5 seeded stops exactly
    Waypoints = new List<GpsPoint>
    {
        new(24.8607, 67.0011),   // Stop 1: Main Bus Terminal
        new(24.8650, 67.0095),   // Stop 2: City Center
        new(24.8700, 67.0175),   // Stop 3: University Gate
        new(24.8755, 67.0260),   // Stop 4: Civic Hospital
        new(24.8810, 67.0340),   // Stop 5: Airport Road End
    }
};

// ─────────────────────────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║         BusTracker — Mock GPS Tracker            ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine($"  Tracker ID  : {config.TrackerId}");
Console.WriteLine($"  API Target  : {config.ApiBaseUrl}");
Console.WriteLine($"  Waypoints   : {config.Waypoints.Count}");
Console.WriteLine($"  Interval    : {config.PingIntervalSeconds}s");
Console.WriteLine();

using var cts    = new CancellationTokenSource();
using var client = new HttpClient { BaseAddress = new Uri(config.ApiBaseUrl) };

// Graceful Ctrl+C shutdown
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n  Stopping tracker...");
    cts.Cancel();
};

int waypointIndex = 0;
int pingCount     = 0;

Console.WriteLine("  Press Ctrl+C to stop.\n");

while (!cts.Token.IsCancellationRequested)
{
    var currentWaypoint = config.Waypoints[waypointIndex];

    // Build the raw JSON payload (must exactly match LocationPingDto)
    var payload = new LocationPingDto
    {
        Latitude     = currentWaypoint.Lat + (Random.Shared.NextDouble() - 0.5) * 0.0002, // tiny GPS noise
        Longitude    = currentWaypoint.Lon + (Random.Shared.NextDouble() - 0.5) * 0.0002,
        TimestampUtc = DateTime.UtcNow
    };

    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    var rawJson  = JsonSerializer.Serialize(payload, jsonOptions);
    var signature = SignPayload(config.TrackerId, rawJson, config.MasterKey);

    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/ping");
        request.Headers.Add("X-Tracker-Id", config.TrackerId);
        request.Headers.Add("X-Signature",  signature);
        request.Content = new StringContent(rawJson, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, cts.Token);

        pingCount++;
        var statusColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
        Console.ForegroundColor = statusColor;
        Console.Write($"  [{DateTime.Now:HH:mm:ss}] Ping #{pingCount:D4}");
        Console.ResetColor();
        Console.Write($"  Waypoint {waypointIndex + 1}/{config.Waypoints.Count}");
        Console.Write($"  Lat: {payload.Latitude:F5}  Lon: {payload.Longitude:F5}");
        Console.WriteLine($"  → {(int)response.StatusCode} {response.ReasonPhrase}");

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"           Response: {errorBody}");
            Console.ResetColor();
        }
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
        Console.ResetColor();
    }

    // Advance to the next waypoint, looping back to the start
    waypointIndex = (waypointIndex + 1) % config.Waypoints.Count;

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(config.PingIntervalSeconds), cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"\n  Mock tracker stopped. Total pings sent: {pingCount}");
Console.ResetColor();

// ─────────────────────────────────────────────────────────────────────────────
//  HMAC-SHA256 Signature (must match TrackerSecurityService logic exactly)
// ─────────────────────────────────────────────────────────────────────────────
static string SignPayload(string trackerId, string rawJson, string masterKey)
{
    // Step 1: Derive device key = HMAC256(masterKey, trackerId)
    using var masterHmac   = new HMACSHA256(Encoding.UTF8.GetBytes(masterKey));
    var       deviceKey    = masterHmac.ComputeHash(Encoding.UTF8.GetBytes(trackerId));

    // Step 2: Sign the payload = HMAC256(deviceKey, rawJson)
    using var payloadHmac  = new HMACSHA256(deviceKey);
    var       hashBytes    = payloadHmac.ComputeHash(Encoding.UTF8.GetBytes(rawJson));

    return Convert.ToBase64String(hashBytes);
}

// ─────────────────────────────────────────────────────────────────────────────
//  Models (mirror of the API models — no project reference needed)
// ─────────────────────────────────────────────────────────────────────────────
record GpsPoint(double Lat, double Lon);

class LocationPingDto
{
    public double   Latitude     { get; set; }
    public double   Longitude    { get; set; }
    public DateTime TimestampUtc { get; set; }
}

class TrackerConfig
{
    public string          ApiBaseUrl          { get; set; } = string.Empty;
    public string          TrackerId           { get; set; } = string.Empty;
    public string          MasterKey           { get; set; } = string.Empty;
    public int             PingIntervalSeconds { get; set; } = 3;
    public List<GpsPoint>  Waypoints           { get; set; } = new();
}
