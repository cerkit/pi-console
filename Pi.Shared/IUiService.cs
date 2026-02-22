using System;
using PiConsole.Models;

namespace PiConsole
{
    public interface IUiService
    {
        void UpdateUiConfig(UiConfigData config);
        void UpdateMenu(MenuItem[] items);
        void UpdatePanel(string targetPanel, string content);
        void AddActiveChannel(string channelName);
    }
}
