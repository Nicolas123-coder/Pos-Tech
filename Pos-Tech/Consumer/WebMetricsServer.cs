using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer
{
    public class WebMetricsServer : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            app.UseRouting();
            app.UseMetricServer();

            await app.StartAsync(stoppingToken);
            await app.WaitForShutdownAsync(stoppingToken);
        }
    }
}