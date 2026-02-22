using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace PiConsole
{
    public class PiCalculusActorService : IHostedService
    {
        private readonly MqttService _mqttService;
        private readonly Engine _engine;

        public PiCalculusActorService(MqttService mqttService, Engine engine)
        {
            _mqttService = mqttService;
            _engine = engine;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Subscribe to the global handshake channel once connected
            _mqttService.Connected += async (sender, args) =>
            {
                await _mqttService.SubscribeAsync($"pi-console/handshake/{_mqttService.ClientId}");
            };

            // Listen for Handshake messages (or any topic really, we filter inside)
            _mqttService.TopicMessageReceived += async (sender, e) =>
            {
                if (e.Topic == $"pi-console/handshake/{_mqttService.ClientId}")
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

                        if (root.TryGetProperty("action", out var actionElement) && actionElement.GetString() == "CONNECT" &&
                            root.TryGetProperty("replyToChannel", out var replyElement))
                        {
                            string? replyToChannel = replyElement.GetString();
                            
                            if (!string.IsNullOrEmpty(replyToChannel))
                            {
                                // Subscribe dynamically to the reply-to channel
                                await _mqttService.SubscribeAsync(replyToChannel);

                                // Notify UI through the Engine
                                _engine.AddActiveChannel(replyToChannel);

                                // Send ACK payload back to the new channel
                                var ackResponse = new { status = "ACK", client = "dotnet-console" };
                                var jsonAck = JsonSerializer.Serialize(ackResponse);
                                await _mqttService.PublishAsync(replyToChannel, jsonAck);
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
        // Deprecated HandshakeRequest object mapping, using JsonDocument directly above for flexibility.
    }
}
