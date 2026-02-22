using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PiConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register our core services
                    services.AddSingleton<MqttService>();
                    services.AddSingleton<Engine>();
                    services.AddSingleton<IUiService>(sp => sp.GetRequiredService<Engine>());

                    // Register background services
                    services.AddHostedService<PiCalculusActorService>();
                    services.AddHostedService<DynamicUiOrchestratorService>();
                })
                .Build();

            // Start the host which spins up PiCalculusActorService in the background
            await host.StartAsync();

            // Run our main terminal UI loop
            var engine = host.Services.GetRequiredService<Engine>();
            engine.Run();

            // When Engine exits, stop the host gracefully
            await host.StopAsync();
        }
    }
}
