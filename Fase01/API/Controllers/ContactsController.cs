using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly ContactService _contactService;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(ContactService contactService, ILogger<ContactsController> logger)
        {
            _contactService = contactService;
            _logger = logger;
        }

        // Endpoint para obter todos os contatos
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Obtendo todos os contatos");
            var contacts = await _contactService.GetContactsByRegionAsync(null); // Passa null para obter todos
            return Ok(contacts);
        }

        // Endpoint para obter um contato específico por ID
        [HttpGet("{id}", Name = "GetContactById")]
        public async Task<IActionResult> GetContactById(int id)
        {
            _logger.LogInformation($"Obtendo contato com ID {id}");
            var contact = await _contactService.GetContactByIdAsync(id);
            if (contact == null)
            {
                return NotFound($"Contato com ID {id} não encontrado.");
            }
            return Ok(contact);
        }

        // Endpoint para obter contatos por região (DDD)
        [HttpGet("region/{regionCode}", Name = "GetContactsByRegion")]
        public async Task<IActionResult> GetContactsByRegion(string regionCode)
        {
            _logger.LogInformation($"Obtendo contatos da região {regionCode}");
            var contacts = await _contactService.GetContactsByRegionAsync(regionCode);
            return Ok(contacts);
        }

        // Endpoint para adicionar um novo contato
        [HttpPost]
        public async Task<IActionResult> AddContact([FromBody] ContactDTO contactDto)
        {
            _logger.LogInformation("Adicionando um novo contato");
            try
            {
                var contact = await _contactService.AddContactAsync(contactDto);
                return CreatedAtRoute("GetContactById", new { id = contact.Id }, contact);
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Errors); // Retorna erros de validação
            }
        }

        // Endpoint para atualizar um contato existente
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] ContactDTO contactDto)
        {
            _logger.LogInformation($"Atualizando contato com ID {id}");
            try
            {
                var updatedContact = await _contactService.UpdateContactAsync(id, contactDto);
                return Ok(updatedContact);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Contato com ID {id} não encontrado.");
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Errors); // Retorna erros de validação
            }
        }

        // Endpoint para excluir um contato por ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            _logger.LogInformation($"Excluindo contato com ID {id}");
            try
            {
                await _contactService.DeleteContactAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Contato com ID {id} não encontrado.");
            }
        }
    }
}
