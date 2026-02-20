using MQTTnet;
using MQTTnet.Client;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace PiConsole
{
    public class MqttService
    {
        private IMqttClient _mqttClient;

        public event EventHandler<string> MessageReceived;
        public event EventHandler<string[]> MenuItemsReceived;

        public async Task StartAsync()
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            string ipAddress = "127.0.0.1"; // default
            int port = 1883; // default
            if (File.Exists("secrets.json"))
            {
                var json = File.ReadAllText("secrets.json");
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("MqttIpAddress", out var ipProp))
                {
                    ipAddress = ipProp.GetString() ?? ipAddress;
                }
                if (document.RootElement.TryGetProperty("MqttPort", out var portProp))
                {
                    port = portProp.GetInt32();
                }
            }

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(ipAddress, port)
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                if (topic == "test/signal")
                {
                    MessageReceived?.Invoke(this, payload);
                }
                else if (topic == "pi-console/menu/items")
                {
                    try
                    {
                        var items = JsonSerializer.Deserialize<MenuItem[]>(payload);
                        if (items != null)
                        {
                            var sortedLabels = items.OrderBy(i => i.Id).Select(i => i.Label).ToArray();
                            MenuItemsReceived?.Invoke(this, sortedLabels);
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors for now
                    }
                }

                return Task.CompletedTask;
            };

            await _mqttClient.ConnectAsync(mqttClientOptions, System.Threading.CancellationToken.None);

            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic("test/signal"))
                .WithTopicFilter(f => f.WithTopic("pi-console/menu/items"))
                .Build();

            await _mqttClient.SubscribeAsync(mqttSubscribeOptions, System.Threading.CancellationToken.None);
        }
    }
}
