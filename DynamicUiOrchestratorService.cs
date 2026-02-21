using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using PiConsole.Models;

namespace PiConsole
{
    public class DynamicUiOrchestratorService : IHostedService
    {
        private readonly MqttService _mqttService;
        private readonly Engine _engine;

        public DynamicUiOrchestratorService(MqttService mqttService, Engine engine)
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

                        if (root.TryGetProperty("action", out var actionElement) && actionElement.GetString() == "INITIATE_SESSION" &&
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
                else if (e.Topic.StartsWith("pi-console/session/"))
                {
                    try
                    {
                        var sessionMsg = JsonSerializer.Deserialize<PiSessionMessage>(e.Payload);
                        if (sessionMsg != null)
                        {
                            if (sessionMsg.MessageType == "UiConfig" && sessionMsg.Data != null)
                            {
                                var uiConfig = JsonSerializer.Deserialize<UiConfigData>(sessionMsg.Data.ToString() ?? "{}");
                                if (uiConfig != null)
                                {
                                    _engine.UpdateUiConfig(uiConfig);
                                    
                                    var uiReadyResponse = new { status = "UI_READY" };
                                    var jsonUiReady = JsonSerializer.Serialize(uiReadyResponse);
                                    await _mqttService.PublishAsync(e.Topic, jsonUiReady);
                                }
                            }
                            else if (sessionMsg.MessageType == "Menu" && sessionMsg.Data != null)
                            {
                                var menuItems = JsonSerializer.Deserialize<MenuItem[]>(sessionMsg.Data.ToString() ?? "[]");
                                if (menuItems != null)
                                {
                                    _engine.UpdateMenu(menuItems);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore parse errors on other messages
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
