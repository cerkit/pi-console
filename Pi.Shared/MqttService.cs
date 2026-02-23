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
        private IMqttClient? _mqttClient;

        public string ClientId { get; set; } = Guid.NewGuid().ToString();

        public bool UseWebSocket { get; set; } = false;
        public string? OverrideMqttServer { get; set; }
        public int? OverrideMqttPort { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<MenuItem[]>? MenuItemsReceived;
        public event EventHandler<(string Topic, string Payload)>? TopicMessageReceived;
        public event EventHandler? Connected;

        public async Task StartAsync()
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            string ipAddress = "127.0.0.1"; // default
            int port = 1883; // default
            string secretsPath = Path.Combine(".secrets", "secrets.json");
            var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
            while (currentDir != null)
            {
                var potentialPath = Path.Combine(currentDir.FullName, ".secrets", "secrets.json");
                if (File.Exists(potentialPath))
                {
                    secretsPath = potentialPath;
                    break;
                }
                currentDir = currentDir.Parent;
            }

            if (File.Exists(secretsPath))
            {
                var json = File.ReadAllText(secretsPath);
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("MqttIpAddress", out var ipProp))
                {
                    ipAddress = ipProp.GetString() ?? ipAddress;
                }
                if (document.RootElement.TryGetProperty("MqttPort", out var portProp))
                {
                    port = portProp.GetInt32();
                }
                if (document.RootElement.TryGetProperty("Username", out var userProp))
                {
                    Username = userProp.GetString() ?? Username;
                }
                if (document.RootElement.TryGetProperty("Password", out var pwdProp))
                {
                    Password = pwdProp.GetString() ?? Password;
                }
            }

            if (!string.IsNullOrEmpty(OverrideMqttServer)) ipAddress = OverrideMqttServer;
            if (OverrideMqttPort.HasValue) port = OverrideMqttPort.Value;

            var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(ClientId);
            
            if (UseWebSocket || ipAddress.StartsWith("ws://") || ipAddress.StartsWith("wss://"))
            {
                string cleanIp = ipAddress.Replace("ws://", "").Replace("wss://", "");
                string wsUri = $"ws://{cleanIp}:{port}/mqtt"; 
                
                mqttClientOptionsBuilder = mqttClientOptionsBuilder
                    .WithWebSocketServer(o => o.WithUri(wsUri));
            }
            else
            {
                mqttClientOptionsBuilder = mqttClientOptionsBuilder.WithTcpServer(ipAddress, port);
            }

            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                mqttClientOptionsBuilder = mqttClientOptionsBuilder.WithCredentials(Username, Password);
            }

            var mqttClientOptions = mqttClientOptionsBuilder.Build();

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                if (topic == "pi-console/status")
                {
                    MessageReceived?.Invoke(this, payload);
                }
                else if (topic == "pi-console/menu/items" || topic.StartsWith("pi-console/session/"))
                {
                    try
                    {
                        // Safely ignore object payloads (like handshakes/acks) by checking if it looks like an array
                        if (payload.TrimStart().StartsWith("["))
                        {
                            var items = JsonSerializer.Deserialize<MenuItem[]>(payload);
                            if (items != null)
                            {
                                var sortedItems = items.OrderBy(i => i.Id).ToArray();
                                MenuItemsReceived?.Invoke(this, sortedItems);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors for now
                    }
                }
                
                // Fire generic topic received for anyone interested
                TopicMessageReceived?.Invoke(this, (topic, payload));

                return Task.CompletedTask;
            };

            await _mqttClient.ConnectAsync(mqttClientOptions, System.Threading.CancellationToken.None);

            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic("pi-console/status"))
                .WithTopicFilter(f => f.WithTopic("pi-console/menu/items"))
                .Build();

            await _mqttClient.SubscribeAsync(mqttSubscribeOptions, System.Threading.CancellationToken.None);
            
            Connected?.Invoke(this, EventArgs.Empty);
        }

        public async Task PublishAsync(string topic, string payload)
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .Build();
                await _mqttClient.PublishAsync(message, System.Threading.CancellationToken.None);
            }
        }

        public async Task SubscribeAsync(string topic)
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                var factory = new MqttFactory();
                var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f.WithTopic(topic))
                    .Build();
                await _mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None);
            }
        }
    }
}
