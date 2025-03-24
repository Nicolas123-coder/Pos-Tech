using MassTransit;
using Application.DTOs;
using Application.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Consumidor
{
    public class EnvelopeConsumer : IConsumer<Envelope>
    {
        private readonly ContactService _contactService;
        private readonly ILogger<EnvelopeConsumer> _logger;

        public EnvelopeConsumer(ContactService contactService, ILogger<EnvelopeConsumer> logger)
        {
            _contactService = contactService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<Envelope> context)
        {
            Console.WriteLine("Chegou mensagem!!!!!!!!.");

            var message = context.Message;

            try
            {
                if (message.Method == "POST")
                {
                    _logger.LogInformation("Processando criação de contato!!.");

                    await _contactService.AddContactAsync(message.Message);

                    _logger.LogInformation("Contato criado com sucesso.");
                }
                else if (message.Method == "PUT")
                {
                    // Supondo que a rota seja do tipo "contacts/{id}"
                    var parts = message.Route.Split('/');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int id))
                    {
                        await _contactService.UpdateContactAsync(id, message.Message);
                        _logger.LogInformation("Contato atualizado com sucesso.");
                    }
                    else
                    {
                        _logger.LogError("Rota inválida para PUT: {Route}", message.Route);
                    }
                }
                else if (message.Method == "DELETE")
                {
                    var parts = message.Route.Split('/');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int id))
                    {
                        await _contactService.DeleteContactAsync(id);
                        _logger.LogInformation("Contato deletado com sucesso.");
                    }
                    else
                    {
                        _logger.LogError("Rota inválida para DELETE: {Route}", message.Route);
                    }
                }
                else
                {
                    _logger.LogWarning("Método não reconhecido: {Method}", message.Method);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar a mensagem");
                throw new Exception("Erro ao processar a mensagem", ex);
            }
        }
    }
}
