using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly ApplicationDbContext _context;

        public ContactRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Contact> AddContactAsync(Contact contact)
        {
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
            return contact;
        }

        public async Task<IEnumerable<Contact>> GetAllContactsAsync()
        {
            return await _context.Contacts.ToListAsync();
        }

        public async Task<IEnumerable<Contact>> GetContactsByRegionAsync(string regionCode)
        {
            return await _context.Contacts
                .Where(c => c.RegionCode == regionCode)
                .ToListAsync();
        }

        public async Task<Contact?> GetContactByIdAsync(int contactId)
        {
            return await _context.Contacts.FindAsync(contactId);
        }

        public async Task<Contact> UpdateContactAsync(Contact contact)
        {
            _context.Contacts.Update(contact);
            await _context.SaveChangesAsync();
            return contact;
        }

        public async Task DeleteContactAsync(int contactId)
        {
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
        }
    }
}
