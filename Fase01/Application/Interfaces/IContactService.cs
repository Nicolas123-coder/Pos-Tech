using Domain.Entities;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IContactService
    {
        Task<Contact> AddContactAsync(ContactDTO contactDto);
        Task<IEnumerable<Contact>> GetContactsByRegionAsync(string? regionCode = null);
        Task<Contact?> GetContactByIdAsync(int contactId);
        Task<Contact> UpdateContactAsync(int contactId, ContactDTO contactDto);
        Task DeleteContactAsync(int contactId);
    }
}