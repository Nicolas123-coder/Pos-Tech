using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Net;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly ILogger<ContactsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ContactsController(ILogger<ContactsController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private void SendMessageToRabbitMQ(string method, string route, ContactDTO message)
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            Console.WriteLine("USANDO PORTA 5672 E HOST rabbitmq");

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare("contacts-exchange", ExchangeType.Direct, durable: true);
                channel.QueueDeclare("contacts-queue", durable: true, exclusive: false, autoDelete: false);
                channel.QueueBind("contacts-queue", "contacts-exchange", "create");

                var messageObject = new Envelope
                {
                    Method = method,
                    Route = route,
                    Message = message
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageObject));

                channel.BasicPublish(
                    exchange: "contacts-exchange",
                    routingKey: "create",
                    basicProperties: null,
                    body: body
                );

                _logger.LogInformation("Mensagem publicada com sucesso na fila.");
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Obtendo contatos via Azure Function");

            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync("http://get-contacts-fn/api/contacts");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Azure Function falhou: {Status}", response.StatusCode);
                return StatusCode((int)response.StatusCode, "Erro ao consultar contatos.");
            }

            var contacts = await response.Content.ReadFromJsonAsync<IEnumerable<ContactDTO>>();
            return Ok(contacts);
        }

        [HttpGet("{id}", Name = "GetContactById")]
        public async Task<IActionResult> GetContactById(int id)
        {
            _logger.LogInformation($"Obtendo contato com ID {id} via Azure Function");

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://get-contacts-fn/api/contacts/{id}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Contato com ID {id} não encontrado.");
                return NotFound($"Contato com ID {id} não encontrado.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Azure Function falhou: {Status}", response.StatusCode);
                return StatusCode((int)response.StatusCode, "Erro ao consultar contato.");
            }

            var contact = await response.Content.ReadFromJsonAsync<ContactDTO>();
            return Ok(contact);
        }

        [HttpGet("region/{regionCode}", Name = "GetContactsByRegion")]
        public async Task<IActionResult> GetContactsByRegion(string regionCode)
        {
            _logger.LogInformation($"Obtendo contatos da região {regionCode} via Azure Function");

            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync($"http://get-contacts-fn/api/contacts/region/{regionCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Azure Function falhou: {Status}", response.StatusCode);
                return StatusCode((int)response.StatusCode, "Erro ao consultar contatos por região.");
            }

            var contacts = await response.Content.ReadFromJsonAsync<IEnumerable<ContactDTO>>();
            return Ok(contacts);
        }

        [HttpPost]
        public async Task<IActionResult> AddContact([FromBody] ContactDTO contactDto)
        {
            _logger.LogInformation("Adicionando um novo contato");

            try
            {
                // Enviar para RabbitMQ
                SendMessageToRabbitMQ("POST", "contacts", contactDto);

                return Ok("Mensagem enviada para o RabbitMQ.");
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Errors);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] ContactDTO contactDto)
        {
            _logger.LogInformation($"Atualizando contato com ID {id}");

            try
            {
                // Enviar para RabbitMQ
                SendMessageToRabbitMQ("PUT", $"contacts/{id}", contactDto);

                return Ok("Mensagem enviada para o RabbitMQ.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Contato com ID {id} não encontrado.");
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Errors);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            _logger.LogInformation($"Excluindo contato com ID {id}");

            try
            {
                // Enviar para RabbitMQ
                SendMessageToRabbitMQ("DELETE", $"contacts/{id}", new ContactDTO { });

                return Ok("Mensagem enviada para o RabbitMQ.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Contato com ID {id} não encontrado.");
            }
        }
    }
}


//using Application.DTOs;
//using MassTransit;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using FluentValidation;

//namespace API.Controllers
//{
//    [ApiController]
//    [Route("[controller]")]
//    public class ContactsController : ControllerBase
//    {
//        private readonly ILogger<ContactsController> _logger;
//        private readonly IPublishEndpoint _publishEndpoint;

//        public ContactsController(IPublishEndpoint publishEndpoint, ILogger<ContactsController> logger)
//        {
//            _publishEndpoint = publishEndpoint;
//            _logger = logger;
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetAll()
//        {
//            _logger.LogInformation("Obtendo todos os contatos (enviando mensagem via MassTransit)");

//            var envelope = new Envelope
//            {
//                Method = "GET",
//                Route = "contacts",
//                Message = null
//            };

//            await _publishEndpoint.Publish(envelope);
//            return Ok("Mensagem enviada via MassTransit.");
//        }

//        [HttpGet("{id}", Name = "GetContactById")]
//        public async Task<IActionResult> GetContactById(int id)
//        {
//            _logger.LogInformation($"Obtendo contato com ID {id}");

//            var envelope = new Envelope
//            {
//                Method = "GET",
//                Route = $"contacts/{id}",
//                Message = null
//            };

//            await _publishEndpoint.Publish(envelope);
//            return Ok("Mensagem enviada via MassTransit.");
//        }

//        [HttpGet("region/{regionCode}", Name = "GetContactsByRegion")]
//        public async Task<IActionResult> GetContactsByRegion(string regionCode)
//        {
//            _logger.LogInformation($"Obtendo contatos da região {regionCode}");

//            var envelope = new Envelope
//            {
//                Method = "GET",
//                Route = $"contacts/region/{regionCode}",
//                Message = null
//            };

//            await _publishEndpoint.Publish(envelope);
//            return Ok("Mensagem enviada via MassTransit.");
//        }

//        [HttpPost]
//        public async Task<IActionResult> AddContact([FromBody] ContactDTO contactDto)
//        {
//            _logger.LogInformation("Adicionando um novo contato");

//            try
//            {
//                var envelope = new Envelope
//                {
//                    Method = "POST",
//                    Route = "contacts",
//                    Message = contactDto
//                };

//                await _publishEndpoint.Publish(envelope);
//                return Ok("Mensagem enviada via MassTransit.");
//            }
//            catch (ValidationException ex)
//            {
//                return BadRequest(ex.Errors);
//            }
//        }

//        [HttpPut("{id}")]
//        public async Task<IActionResult> UpdateContact(int id, [FromBody] ContactDTO contactDto)
//        {
//            _logger.LogInformation($"Atualizando contato com ID {id}");

//            try
//            {
//                var envelope = new Envelope
//                {
//                    Method = "PUT",
//                    Route = $"contacts/{id}",
//                    Message = contactDto
//                };

//                await _publishEndpoint.Publish(envelope);
//                return Ok("Mensagem enviada via MassTransit.");
//            }
//            catch (KeyNotFoundException)
//            {
//                return NotFound($"Contato com ID {id} não encontrado.");
//            }
//            catch (ValidationException ex)
//            {
//                return BadRequest(ex.Errors);
//            }
//        }

//        [HttpDelete("{id}")]
//        public async Task<IActionResult> DeleteContact(int id)
//        {
//            _logger.LogInformation($"Excluindo contato com ID {id}");

//            try
//            {
//                var envelope = new Envelope
//                {
//                    Method = "DELETE",
//                    Route = $"contacts/{id}",
//                    Message = null
//                };

//                await _publishEndpoint.Publish(envelope);
//                return Ok("Mensagem enviada via MassTransit.");
//            }
//            catch (KeyNotFoundException)
//            {
//                return NotFound($"Contato com ID {id} não encontrado.");
//            }
//        }
//    }
//}
