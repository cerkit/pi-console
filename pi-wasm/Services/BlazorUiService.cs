using System;
using System.Collections.Generic;
using PiConsole;
using Pi.Shared.Models;

namespace pi_wasm.Services
{
    public class BlazorUiService : IUiService
    {
        public event Action? OnStateChanged;

        private readonly MqttService _mqttService;

        public BlazorUiService(MqttService mqttService)
        {
            _mqttService = mqttService;
        }

        public UiConfigData? UiConfig { get; private set; }
        public MenuItem[] MenuItems { get; private set; } = Array.Empty<MenuItem>();
        public List<string> ActiveChannels { get; private set; } = new List<string>();
        
        public string LastOutputContent { get; private set; } = "";
        public string LastOperationsContent { get; private set; } = "";
        public string LastStatusContent { get; private set; } = "System idle.";
        public string LastHeaderContent { get; private set; } = "";
        public string LastMenuContent { get; private set; } = "";

        public void AddActiveChannel(string channelName)
        {
            if (!ActiveChannels.Contains(channelName))
            {
                ActiveChannels.Add(channelName);
                NotifyStateChanged();
            }
        }

        public void UpdateMenu(MenuItem[] items)
        {
            MenuItems = items;
            NotifyStateChanged();
        }

        public void UpdatePanel(string targetPanel, string content)
        {
            if (targetPanel.Equals("commandProcessor", StringComparison.OrdinalIgnoreCase))
            {
                switch (content.ToUpperInvariant())
                {
                    case "EXIT":
                        // In WebAssembly, we can't truly Environment.Exit, but we can clear the state to simulate a logoff
                        LastStatusContent = "[red]System halted. Please refresh the page.[/]";
                        MenuItems = Array.Empty<MenuItem>();
                        ActiveChannels.Clear();
                        NotifyStateChanged();
                        break;
                    case "RESTART":
                        LastStatusContent = "Restarting UI configuration sequence...";
                        NotifyStateChanged();

                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            try
                            {
                                var p = new { clientId = _mqttService.ClientId };
                                await _mqttService.PublishAsync("pi-console/client/startup", System.Text.Json.JsonSerializer.Serialize(p));
                            }
                            catch (Exception ex)
                            {
                                LastStatusContent = $"[red]Restart err:[/] {pi_wasm.Helpers.SpectreConsoleParser.ParseToHtml("[red]" + ex.Message + "[/]")}";
                                NotifyStateChanged();
                            }
                        });
                        break;
                }
                return;
            }

            var parsedContent = pi_wasm.Helpers.SpectreConsoleParser.ParseToHtml(content);

            switch(targetPanel.ToLowerInvariant())
            {
                case "outputpanel":
                    LastOutputContent = parsedContent;
                    break;
                case "operationspanel":
                    LastOperationsContent = parsedContent;
                    break;
                case "statuspanel":
                    LastStatusContent = parsedContent;
                    break;
                case "headerpanel":
                    LastHeaderContent = parsedContent;
                    break;
                case "menupanel":
                    LastMenuContent = parsedContent;
                    break;
            }
            NotifyStateChanged();
        }

        public void UpdateUiConfig(UiConfigData config)
        {
            UiConfig = config;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}
