using Microsoft.EntityFrameworkCore;
using ClientXMLApp.Data;
using ClientXMLApp.Models;

namespace ClientXMLApp.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly AppDbContext _context;

        public AddressRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Address>> GetAllAddressesAsync()
        {
            return await _context.Addresses.ToListAsync();
        }

        public async Task AddAddressAsync(Address address)
        {
            await _context.Addresses.AddAsync(address);
        }

        public async Task UpdateAddressAsync(Address address)
        {
            _context.Addresses.Update(address);
        }

        public async Task DeleteAddressAsync(int id)
        {
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.ID == id);
            if (address != null)
            {
                _context.Addresses.Remove(address);
            }
        }

        public async Task<IEnumerable<Address>> GetAllAddressesForClientAsync(int clientId)
        {
            return await _context.Addresses
                .Where(a => a.ClientID == clientId)
                .ToListAsync();
        }
    }
}