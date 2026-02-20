using MQTTnet;
using MQTTnet.Client;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PiConsole
{
    public class MqttService
    {
        private IMqttClient _mqttClient;

        public event EventHandler<string> MessageReceived;

        public async Task StartAsync()
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            string ipAddress = "127.0.0.1"; // default
            if (File.Exists("secrets.json"))
            {
                var json = File.ReadAllText("secrets.json");
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("MqttIpAddress", out var ipProp))
                {
                    ipAddress = ipProp.GetString() ?? ipAddress;
                }
            }

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(ipAddress, 1883)
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                if (topic == "test/signal")
                {
                    MessageReceived?.Invoke(this, payload);
                }

                return Task.CompletedTask;
            };

            await _mqttClient.ConnectAsync(mqttClientOptions, System.Threading.CancellationToken.None);

            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic("test/signal"))
                .Build();

            await _mqttClient.SubscribeAsync(mqttSubscribeOptions, System.Threading.CancellationToken.None);
        }
    }
}
