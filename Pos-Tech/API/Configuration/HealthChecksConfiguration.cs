using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace API.Configuration
{
    public static class HealthChecksConfiguration
    {
        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, string connectionString)
        {
            services.AddHealthChecks()
                .AddSqlServer(
                    connectionString,
                    healthQuery: "SELECT 1;",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "db", "sql", "sqlserver" })
                .AddCheck<AzureFunctionHealthCheck>(
                    "azure-function",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "function", "getcontacts" });

            return services;
        }

        public static IApplicationBuilder UseCustomHealthChecks(this IApplicationBuilder app)
        {
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            status = report.Status.ToString(),
                            checks = report.Entries.Select(e => new
                            {
                                name = e.Key,
                                status = e.Value.Status.ToString(),
                                description = e.Value.Description,
                                duration = e.Value.Duration.ToString(),
                                tags = e.Value.Tags
                            })
                        });
                    await context.Response.WriteAsync(result);
                }
            });

            return app;
        }
    }

    public class AzureFunctionHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AzureFunctionHealthCheck(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("http://get-contacts-service.contacts-app/api/contacts", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Azure Function is healthy");
                }

                return HealthCheckResult.Degraded($"Azure Function returned {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Azure Function is unhealthy", ex);
            }
        }
    }
}