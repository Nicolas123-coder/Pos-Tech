using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Text;
using Application.Services;
using Application.DTOs;
using RabbitMQ.Client;

namespace ContactsMS
{
    public class CreateContactFunction
    {
        private readonly ContactService _contactService;
        private readonly ILogger _logger;

        public CreateContactFunction(ContactService contactService, ILoggerFactory loggerFactory)
        {
            _contactService = contactService;
            _logger = loggerFactory.CreateLogger<CreateContactFunction>();
        }

        [Function("CreateContact")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contacts")] HttpRequestData req)
        {
            _logger.LogInformation("Recebendo solicitação de criação de contato...");

            var body = await req.ReadAsStringAsync();
            var contactDto = JsonSerializer.Deserialize<ContactDTO>(body);

            try
            {
                // Validação
                await _contactService.ValidateAsync(contactDto);

                // Preparar mensagem
                var message = new
                {
                    operation = "create",
                    data = contactDto
                };

                var messageBody = JsonSerializer.Serialize(message);
                var bodyBytes = Encoding.UTF8.GetBytes(messageBody);

                // Publicar no RabbitMQ
                var factory = new ConnectionFactory { HostName = "localhost" }; // ou variável env
                using var connection = factory.CreateConnectionAsync();
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare("contacts-exchange", ExchangeType.Direct, durable: true);
                channel.QueueDeclare("create-contact-queue", durable: true, exclusive: false, autoDelete: false);
                channel.QueueBind("create-contact-queue", "contacts-exchange", "create");

                var props = channel.CreateBasicProperties();
                props.Persistent = true;

                channel.BasicPublish("contacts-exchange", "create", props, bodyBytes);

                _logger.LogInformation("Contato publicado com sucesso na fila");

                var response = req.CreateResponse(HttpStatusCode.Accepted);
                await response.WriteStringAsync("Contato enviado para processamento.");
                return response;
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Validação falhou: {0}", ex.Message);

                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(ex.Errors);
                return response;
            }
        }
    }
}