using Domain.Entities;
using Domain.Interfaces;
using Application.DTOs;
using FluentValidation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ContactService
    {
        private readonly IContactRepository _contactRepository;
        private readonly IValidator<ContactDTO> _validator;

        public ContactService(IContactRepository contactRepository, IValidator<ContactDTO> validator)
        {
            _contactRepository = contactRepository;
            _validator = validator;
        }

        public async Task<Contact> AddContactAsync(ContactDTO contactDto)
        {
            var validationResult = await _validator.ValidateAsync(contactDto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var contact = new Contact(contactDto.Name, contactDto.Phone, contactDto.Email, contactDto.RegionCode);
            return await _contactRepository.AddContactAsync(contact);
        }

        public async Task<IEnumerable<Contact>> GetContactsByRegionAsync(string? regionCode = null)
        {
            return regionCode == null
                ? await _contactRepository.GetAllContactsAsync()
                : await _contactRepository.GetContactsByRegionAsync(regionCode);
        }

        public async Task<Contact?> GetContactByIdAsync(int contactId)
        {
            return await _contactRepository.GetContactByIdAsync(contactId);
        }

        public async Task<Contact> UpdateContactAsync(int contactId, ContactDTO contactDto)
        {
            var contact = await _contactRepository.GetContactByIdAsync(contactId);
            if (contact == null)
            {
                throw new KeyNotFoundException($"Contato com ID {contactId} não encontrado.");
            }

            var validationResult = await _validator.ValidateAsync(contactDto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            contact.Name = contactDto.Name;
            contact.Phone = contactDto.Phone;
            contact.Email = contactDto.Email;
            contact.RegionCode = contactDto.RegionCode;

            return await _contactRepository.UpdateContactAsync(contact);
        }

        public async Task DeleteContactAsync(int contactId)
        {
            var contact = await _contactRepository.GetContactByIdAsync(contactId);
            if (contact == null)
            {
                throw new KeyNotFoundException($"Contato com ID {contactId} não encontrado.");
            }

            await _contactRepository.DeleteContactAsync(contactId);
        }
    }
}
