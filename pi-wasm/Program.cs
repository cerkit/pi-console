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
    service.OverrideMqttPort = 1880; // Default Node-RED websocket port is usually same as Node-RED, e.g. 1880, but MQTT broker in Node-RED Aedes uses WS.
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
    await mqttService.PublishAsync("pi-console/client/startup", "{ \"status\": \"online\" }");
});

await host.RunAsync();
