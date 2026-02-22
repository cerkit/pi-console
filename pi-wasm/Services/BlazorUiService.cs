using System;
using System.Collections.Generic;
using PiConsole;
using PiConsole.Models;

namespace pi_wasm.Services
{
    public class BlazorUiService : IUiService
    {
        public event Action? OnStateChanged;

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
            switch(targetPanel.ToLowerInvariant())
            {
                case "outputpanel":
                    LastOutputContent = content;
                    break;
                case "operationspanel":
                    LastOperationsContent = content;
                    break;
                case "statuspanel":
                    LastStatusContent = content;
                    break;
                case "headerpanel":
                    LastHeaderContent = content;
                    break;
                case "menupanel":
                    LastMenuContent = content;
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
