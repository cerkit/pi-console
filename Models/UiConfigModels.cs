using System.Text.Json.Serialization;

namespace PiConsole.Models
{
    public class PiSessionMessage
    {
        [JsonPropertyName("messageType")]
        public string MessageType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    public class UiConfigData
    {
        [JsonPropertyName("headerPanel")]
        public PanelConfig? HeaderPanel { get; set; }

        [JsonPropertyName("operationsPanel")]
        public PanelConfig? OperationsPanel { get; set; }

        [JsonPropertyName("statusPanel")]
        public PanelConfig? StatusPanel { get; set; }

        [JsonPropertyName("menuPanel")]
        public PanelConfig? MenuPanel { get; set; }

        [JsonPropertyName("outputPanel")]
        public PanelConfig? OutputPanel { get; set; }
    }

    public class PanelConfig
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("borderColor")]
        public string? BorderColor { get; set; }

        [JsonPropertyName("titleColor")]
        public string? TitleColor { get; set; }
    }
}
