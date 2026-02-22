using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using PiConsole.Models;
using Spectre.Console;

namespace PiConsole
{
    public class DynamicUiOrchestratorService : IHostedService
    {
        private readonly MqttService _mqttService;
        private readonly IUiService _uiService;

        public DynamicUiOrchestratorService(MqttService mqttService, IUiService uiService)
        {
            _mqttService = mqttService;
            _uiService = uiService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mqttService.Connected += async (sender, args) =>
            {
                await _mqttService.SubscribeAsync($"pi-console/handshake/{_mqttService.ClientId}");
            };

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

                        if (root.TryGetProperty("action", out var actionElement) && actionElement.GetString() == "INITIATE_SESSION" &&
                            root.TryGetProperty("channel", out var channelElement))
                        {
                            string? channel = channelElement.GetString();
                            if (!string.IsNullOrEmpty(channel))
                            {
                                // Subscribe dynamically to the dynamic menu channel
                                await _mqttService.SubscribeAsync(channel);

                                // Notify UI through the Engine
                                _uiService.AddActiveChannel(channel);

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
                                    _uiService.UpdateUiConfig(uiConfig);
                                    
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
                                    _uiService.UpdateMenu(menuItems);
                                }
                            }
                            else if (sessionMsg.MessageType == "PanelUpdate" && sessionMsg.Data != null)
                            {
                                string rawJson = "";
                                if (sessionMsg.Data is JsonElement dataElement)
                                {
                                    rawJson = dataElement.GetRawText();
                                }
                                else
                                {
                                    rawJson = sessionMsg.Data.ToString() ?? "{}";
                                }

                                var updateData = JsonSerializer.Deserialize<PanelUpdateData>(rawJson);
                                if (updateData != null && !string.IsNullOrEmpty(updateData.TargetPanel) && !string.IsNullOrEmpty(updateData.Content))
                                {
                                    _uiService.UpdatePanel(updateData.TargetPanel, updateData.Content);
                                }
                                else
                                {
                                    _uiService.UpdatePanel("outputPanel", $"[red]Failed to parse PanelUpdate:[/] JSON: {Markup.Escape(rawJson)}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Fallback debug to status
                        _uiService.UpdatePanel("outputPanel", $"[red]Parser Err:[/] {Markup.Escape(ex.Message)}");
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
