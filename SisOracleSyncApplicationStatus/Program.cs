using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SisOracleSyncApplicationStatus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(@"c:\ActiveCampainInetgWithCams/Log_AppStatus.txt")
                .CreateLogger();

            try
            {
                Log.Information("Service had Started");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception Ex)
            {
                Log.Fatal(Ex, "Error Occured in service");
                throw;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                }).UseSerilog();
    }
}
