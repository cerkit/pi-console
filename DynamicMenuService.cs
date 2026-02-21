using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace PiConsole
{
    public class DynamicMenuService : IHostedService
    {
        private readonly MqttService _mqttService;
        private readonly Engine _engine;

        public DynamicMenuService(MqttService mqttService, Engine engine)
        {
            _mqttService = mqttService;
            _engine = engine;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mqttService.Connected += async (sender, args) =>
            {
                await _mqttService.SubscribeAsync("pi-console/handshake");
            };

            _mqttService.TopicMessageReceived += async (sender, e) =>
            {
                if (e.Topic == "pi-console/handshake")
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(e.Payload);
                        var root = doc.RootElement;
                        
                        // Handle case where Node-RED sends the entire msg object
                        if (root.TryGetProperty("payload", out var payloadElement) && payloadElement.ValueKind == JsonValueKind.Object)
                        {
                            root = payloadElement;
                        }

                        if (root.TryGetProperty("action", out var actionElement) && actionElement.GetString() == "PROVIDE_MENU" &&
                            root.TryGetProperty("channel", out var channelElement))
                        {
                            string? channel = channelElement.GetString();
                            if (!string.IsNullOrEmpty(channel))
                            {
                                // Subscribe dynamically to the dynamic menu channel
                                await _mqttService.SubscribeAsync(channel);

                                // Notify UI through the Engine
                                _engine.AddActiveChannel(channel);

                                // Send READY payload back to the new channel
                                var readyResponse = new { status = "READY" };
                                var jsonReady = JsonSerializer.Serialize(readyResponse);
                                await _mqttService.PublishAsync(channel, jsonReady);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore bad handshakes
                     }
                }
            };

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
