using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;
using UserFlow.API.ChangesStreams.Hubs;
using UserFlow.API.Shared.Notifications;

namespace UserFlow.API.ChangeStreams.Services;

/// <summary>
/// 🎧 BackgroundService that listens to PostgreSQL NOTIFY events and broadcasts them to connected SignalR clients.
/// </summary>
public class DatabaseChangeService : BackgroundService
{
    private readonly IHubContext<ChangeHub> _hubContext;
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseChangeService> _logger;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// 🏗️ Constructor injecting all required services.
    /// </summary>
    public DatabaseChangeService(
        IHubContext<ChangeHub> hubContext,
        IConfiguration config,
        ILogger<DatabaseChangeService> logger)
    {
        _hubContext = hubContext;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// 🚀 Executes the background task that listens to PostgreSQL NOTIFY and forwards to SignalR clients.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _config.GetConnectionString("DefaultConnection");

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(stoppingToken);

        conn.Notification += async (o, e) =>
        {
            try
            {
                var notification = JsonSerializer.Deserialize<ChangeNotification>(e.Payload, _serializerOptions);

                if (notification == null || string.IsNullOrWhiteSpace(notification.EntityName))
                {
                    _logger.LogError("❌ Invalid or null notification. Skipping.");
                    return;
                }

                _logger.LogInformation($"📥 Received SignalR Notification: EntityName = \"{notification.EntityName}\" - EntityId = \"{notification.EntityId}\" - Operation = \"{notification.Operation}\"");

                await _hubContext.Clients.Group(notification.EntityName)
                    .SendAsync("ReceiveChange", notification);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error handling notification: {ex}");
            }
        };

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "LISTEN table_changed;";
            await cmd.ExecuteNonQueryAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await conn.WaitAsync(stoppingToken);
        }
    }
}
