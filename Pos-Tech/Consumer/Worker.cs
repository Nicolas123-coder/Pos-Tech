using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Application.DTOs;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Consumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                "contacts-queue",
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (model, ea) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var contactService = scope.ServiceProvider.GetRequiredService<ContactService>();

                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var envelope = JsonSerializer.Deserialize<Envelope>(json);

                    if (envelope == null)
                    {
                        _logger.LogWarning("Envelope nulo recebido.");
                        return;
                    }

                    var method = envelope.Method?.ToUpperInvariant();
                    var route = envelope.Route ?? string.Empty;
                    var message = envelope.Message;

                    switch (method)
                    {
                        case "POST":
                            _logger.LogInformation("Processando criação de contato.");
                            await contactService.AddContactAsync(message);
                            _logger.LogInformation("Contato criado com sucesso.");
                            break;

                        case "PUT":
                            if (TryGetIdFromRoute(route, out int putId))
                            {
                                await contactService.UpdateContactAsync(putId, message);
                                _logger.LogInformation("Contato atualizado com sucesso.");
                            }
                            else
                            {
                                _logger.LogError("Rota inválida para PUT: {Route}", route);
                            }
                            break;

                        case "DELETE":
                            if (TryGetIdFromRoute(route, out int deleteId))
                            {
                                await contactService.DeleteContactAsync(deleteId);
                                _logger.LogInformation("Contato deletado com sucesso.");
                            }
                            else
                            {
                                _logger.LogError("Rota inválida para DELETE: {Route}", route);
                            }
                            break;

                        default:
                            _logger.LogWarning("Método não reconhecido: {Method}", method);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar a mensagem");
                }
            };

            channel.BasicConsume(
                queue: "contacts-queue",
                autoAck: true,
                consumer: consumer
            );

            _logger.LogInformation("👂 Worker aguardando mensagens...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private bool TryGetIdFromRoute(string route, out int id)
        {
            id = 0;

            var parts = route.Split('/');
            return parts.Length >= 2 && int.TryParse(parts[1], out id);
        }

    }
}
