using System.Text.Json.Serialization;

namespace PiConsole
{
    public class MenuItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty; // Default to terminal scheme

        [JsonPropertyName("actionTopic")]
        public string? ActionTopic { get; set; }
    }
}
