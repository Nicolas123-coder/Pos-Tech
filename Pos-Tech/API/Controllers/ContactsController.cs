using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using System.Text;
using System.Text.Json;
using System.Net;
using RabbitMQ.Client;
using Infrastructure.Messaging;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly ILogger<ContactsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IModel _channel;

        public ContactsController(
            ILogger<ContactsController> logger,
            IHttpClientFactory httpClientFactory,
            IModel channel)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _channel = channel;
        }

        private void SendMessageToRabbitMQ(string method, string route, ContactDTO message)
        {
            _logger.LogInformation("Enviando mensagem para o RabbitMQ");

            var messageObject = new Envelope
            {
                Method = method,
                Route = route,
                Message = message
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageObject));

            _channel.BasicPublish(
                exchange: RabbitMQConfiguration.ExchangeName,
                routingKey: RabbitMQConfiguration.RoutingKey,
                basicProperties: null,
                body: body
            );

            _logger.LogInformation("Mensagem publicada com sucesso na fila.");
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
                SendMessageToRabbitMQ("POST", "contacts", contactDto);

                return Ok("Mensagem enviada para o RabbitMQ.");
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem para o RabbitMQ");
                return StatusCode(500, "Erro interno ao processar a solicitação");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] ContactDTO contactDto)
        {
            _logger.LogInformation($"Atualizando contato com ID {id}");

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem para o RabbitMQ");
                return StatusCode(500, "Erro interno ao processar a solicitação");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            _logger.LogInformation($"Excluindo contato com ID {id}");

            try
            {
                SendMessageToRabbitMQ("DELETE", $"contacts/{id}", new ContactDTO { });

                return Ok("Mensagem enviada para o RabbitMQ.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Contato com ID {id} não encontrado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem para o RabbitMQ");
                return StatusCode(500, "Erro interno ao processar a solicitação");
            }
        }
    }
}