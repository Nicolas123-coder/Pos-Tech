using Application.DTOs;
using Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CreateContactConsumerFunction
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ContactService _contactService;
        private IConnection? _connection;
        private IChannel? _channel;

        public Worker(ILogger<Worker> logger, ContactService contactService)
        {
            _logger = logger;
            _contactService = contactService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName"),
                UserName = Environment.GetEnvironmentVariable("RabbitMQ__UserName"),
                Password = Environment.GetEnvironmentVariable("RabbitMQ__Password")
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync("contacts-exchange", ExchangeType.Direct, durable: true);
            await _channel.QueueDeclareAsync("create-contact-queue", durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync("create-contact-queue", "contacts-exchange", "create");

            _logger.LogInformation("🎧 Conectado à fila create-contact-queue");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            // Substituindo o evento "Received" pela manipulação direta de mensagens
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("📨 Mensagem recebida: {0}", messageJson);

                    var envelope = JsonSerializer.Deserialize<Envelope>(messageJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (envelope?.Operation == "create" && envelope.Data != null)
                    {
                        await _contactService.AddContactAsync(envelope.Data);
                        _logger.LogInformation("✅ Contato salvo com sucesso.");
                    }
                    else
                    {
                        _logger.LogWarning("❌ Envelope inválido ou operação não suportada.");
                    }

                    // Confirma o processamento da mensagem
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Erro ao processar mensagem: {0}", ex.Message);
                }
            };

            // Consome a mensagem da fila
            await _channel.BasicConsumeAsync(
                queue: "create-contact-queue",
                autoAck: false,
                consumer: consumer
            );

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _channel?.CloseAsync();
            await _connection?.CloseAsync();
            await base.StopAsync(cancellationToken);
        }

        private class Envelope
        {
            public string Operation { get; set; }
            public ContactDTO Data { get; set; }
        }
    }
}
