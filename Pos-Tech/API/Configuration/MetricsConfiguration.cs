using Prometheus;

namespace API.Configuration
{
    public static class MetricsConfiguration
    {
        public static IServiceCollection AddCustomMetrics(this IServiceCollection services)
        {
            services.AddSingleton<Counter>(
                Metrics.CreateCounter("contacts_api_requests_total", "Total number of requests to the Contacts API"));

            services.AddSingleton<Histogram>(
                Metrics.CreateHistogram("contacts_api_request_duration_seconds",
                "Histogram of API request durations"));

            services.AddSingleton<Gauge>(
                Metrics.CreateGauge("contacts_api_in_progress_requests",
                "Number of requests currently in progress"));

            return services;
        }

        public static IApplicationBuilder UseCustomMetrics(this IApplicationBuilder app)
        {
            app.UseMetricServer();
            app.UseHttpMetrics();

            return app;
        }
    }
}