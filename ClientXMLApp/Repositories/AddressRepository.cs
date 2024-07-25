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

        public async Task<Address> GetAddressByIdAsync(AddressType type, string clientId)
        {
            return await _context.Addresses.FirstOrDefaultAsync(a => a.Type == type && a.ClientID == clientId);
        }

        public async Task AddAddressAsync(Address address)
        {
            await _context.Addresses.AddAsync(address);
        }

        public async Task UpdateAddressAsync(Address address)
        {
            _context.Addresses.Update(address);
        }

        public async Task DeleteAddressAsync(AddressType type, string clientId)
        {
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Type == type && a.ClientID == clientId);
            if (address != null)
            {
                _context.Addresses.Remove(address);
            }
        }
    }
}