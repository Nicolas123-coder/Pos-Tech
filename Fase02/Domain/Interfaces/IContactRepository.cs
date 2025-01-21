using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IContactRepository
    {
        Task<Contact> AddContactAsync(Contact contact);
        Task<IEnumerable<Contact>> GetAllContactsAsync();
        Task<IEnumerable<Contact>> GetContactsByRegionAsync(string regionCode);
        Task<Contact?> GetContactByIdAsync(int contactId);
        Task<Contact> UpdateContactAsync(Contact contact);
        Task DeleteContactAsync(int contactId);
    }
}
