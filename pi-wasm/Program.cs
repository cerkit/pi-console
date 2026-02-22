using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using pi_wasm;
using PiConsole;
using pi_wasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add Pi.Shared Services
builder.Services.AddSingleton<MqttService>(sp => 
{
    var service = new MqttService();
    service.UseWebSocket = true;
    service.OverrideMqttServer = "localhost";
    service.OverrideMqttPort = 9001; // Mosquitto WebSocket port is 9001
    service.ClientId = "pi-wasm";
    return service;
});

builder.Services.AddSingleton<BlazorUiService>();
builder.Services.AddSingleton<IUiService>(sp => sp.GetRequiredService<BlazorUiService>());

// DynamicUiOrchestratorService is an IHostedService. In Blazor WASM, we don't have a background host, so we just instantiate it and call StartAsync manually, or register it and resolve it.
builder.Services.AddSingleton<DynamicUiOrchestratorService>();

var host = builder.Build();

// Start Mqtt and Orchestrator
var mqttService = host.Services.GetRequiredService<MqttService>();
var orchestrator = host.Services.GetRequiredService<DynamicUiOrchestratorService>();

_ = orchestrator.StartAsync(System.Threading.CancellationToken.None);
_ = mqttService.StartAsync().ContinueWith(async t => 
{
    await Task.Delay(500); // Give subscriptions a moment to establish
    var p = new { clientId = mqttService.ClientId };
    await mqttService.PublishAsync("pi-console/client/startup", System.Text.Json.JsonSerializer.Serialize(p));
});

await host.RunAsync();
