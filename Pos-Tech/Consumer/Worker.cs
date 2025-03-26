using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Application.DTOs;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Messaging;
using Prometheus;

namespace Consumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _rabbitConnection;
        private readonly Counter _messagesProcessedCounter;
        private readonly Gauge _processingQueueSize;

        public Worker(
            ILogger<Worker> logger,
            IServiceProvider serviceProvider,
            IConnection rabbitConnection,
            Counter messagesProcessedCounter,
            Gauge processingQueueSize)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _rabbitConnection = rabbitConnection;
            _messagesProcessedCounter = messagesProcessedCounter;
            _processingQueueSize = processingQueueSize;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var channel = _rabbitConnection.CreateModel();

            RabbitMQConfiguration.DeclareQueuesAndExchanges(channel);

            _logger.LogInformation("Consumidor configurado. Filas declaradas.");

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (model, ea) =>
            {
                _processingQueueSize.Inc(); // Incrementar o contador de mensagens em processamento

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

                    _messagesProcessedCounter.Inc();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar a mensagem");
                }
                finally
                {
                    _processingQueueSize.Dec();
                }
            };

            channel.BasicConsume(
                queue: RabbitMQConfiguration.QueueName,
                autoAck: true,
                consumer: consumer
            );

            _logger.LogInformation("Worker aguardando mensagens...");

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