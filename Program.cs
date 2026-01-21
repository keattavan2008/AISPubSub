using AISPubSub.Infrastructure.Api;
using AISPubSub.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AISPubSub
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
                //Custom Configuration
                ApplicationConfiguration.Initialize();

                // Create the builder directly in Main to avoid interface visibility issues
                var host = Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        // Register Services
                        var sqliteConn = context.Configuration.GetConnectionString("SqliteConnection")!;
                        services.AddSingleton<DataAccess>(sp => new DataAccess(sqliteConn));
                        services.AddHttpClient<ApiService>();
                        // Register the Form
                        services.AddTransient<AisApp>();
                    })
                    .Build();

                // Run the application
                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var mainForm = services.GetRequiredService<AisApp>();
                    Application.Run(mainForm);
                }
        }
        
    }
}