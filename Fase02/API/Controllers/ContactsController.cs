using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(ILogger<ContactsController> logger)
        {
            _logger = logger;
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
            _logger.LogInformation("Obtendo todos os contatos (enviando mensagem ao rabbitmq)");

            // Enviar para RabbitMQ (Não fazemos mais a lógica de banco de dados aqui)
            SendMessageToRabbitMQ("GET", "contacts", new ContactDTO { });

            return Ok("Mensagens enviadas para o RabbitMQ.");
        }

        [HttpGet("{id}", Name = "GetContactById")]
        public async Task<IActionResult> GetContactById(int id)
        {
            _logger.LogInformation($"Obtendo contato com ID {id}");

            // Enviar para RabbitMQ
            SendMessageToRabbitMQ("GET", $"contacts/{id}", new ContactDTO { });

            return Ok("Mensagens enviadas para o RabbitMQ.");
        }

        [HttpGet("region/{regionCode}", Name = "GetContactsByRegion")]
        public async Task<IActionResult> GetContactsByRegion(string regionCode)
        {
            _logger.LogInformation($"Obtendo contatos da região {regionCode}");

            // Enviar para RabbitMQ
            SendMessageToRabbitMQ("GET", $"contacts/region/{regionCode}", new ContactDTO { });

            return Ok("Mensagens enviadas para o RabbitMQ.");
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
