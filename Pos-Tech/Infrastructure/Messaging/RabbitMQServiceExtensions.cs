using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Infrastructure.Messaging
{
    public static class RabbitMQServiceExtensions
    {
        public static IServiceCollection AddRabbitMQServices(this IServiceCollection services)
        {
            services.AddSingleton<IConnectionFactory>(sp =>
                RabbitMQConfiguration.CreateConnectionFactory());

            services.AddSingleton<IConnection>(sp =>
            {
                var factory = sp.GetRequiredService<IConnectionFactory>();
                return factory.CreateConnection();
            });

            services.AddScoped<IModel>(sp =>
            {
                var connection = sp.GetRequiredService<IConnection>();
                var channel = connection.CreateModel();

                RabbitMQConfiguration.DeclareQueuesAndExchanges(channel);

                return channel;
            });

            return services;
        }
    }
}