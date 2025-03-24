using Application.DTOs;
using Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CreateContactProducerFunction
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
            _logger.LogInformation("Recebendo novo contato...");

            string requestBody = await req.ReadAsStringAsync();
            ContactDTO contactDto;

            try
            {
                contactDto = JsonSerializer.Deserialize<ContactDTO>(requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (contactDto == null)
                {
                    throw new FluentValidation.ValidationException("Payload inválido.");
                }

                // Valida com FluentValidation via ContactService
                await _contactService.ValidateAsync(contactDto);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning("Validação falhou: {0}", ex.Message);
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ex.Errors);

                return badResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao processar requisição: {0}", ex.Message);
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Erro ao processar requisição.");

                return errorResponse;
            }

            try
            {
                // [Producer Function] → publish("create", data)
                //        |
                //     [contacts - exchange](tipo: direct)
                //        |
                //  routingKey == "create"
                //        ↓
                // [create-contact - queue]

                var factory = new ConnectionFactory
                {
                    HostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName"),
                    UserName = Environment.GetEnvironmentVariable("RabbitMQ__UserName"),
                    Password = Environment.GetEnvironmentVariable("RabbitMQ__Password")
                };
                var connection = await factory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync();

                await channel.ExchangeDeclareAsync("contacts-exchange", ExchangeType.Direct, durable: true);
                await channel.QueueDeclareAsync("create-contact-queue", durable: true, exclusive: false, autoDelete: false);
                await channel.QueueBindAsync("create-contact-queue", "contacts-exchange", "create");

                var message = new
                {
                    operation = "create",
                    data = contactDto
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await channel.BasicPublishAsync(
                    exchange: "contacts-exchange",
                    routingKey: "create",
                    body: body
                );

                _logger.LogInformation("Mensagem publicada com sucesso na fila.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao publicar no RabbitMQ: {0}", ex.Message);
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Erro ao publicar mensagem.");

                return errorResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteStringAsync("Contato enviado para processamento.");

            return response;
        }
    }
}
