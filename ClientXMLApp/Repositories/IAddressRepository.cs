using ClientXMLApp.Models;

namespace ClientXMLApp.Repositories
{
    public interface IAddressRepository
    {
        Task<IEnumerable<Address>> GetAllAddressesAsync();
        Task AddAddressAsync(Address address);
        Task UpdateAddressAsync(Address address);
        Task DeleteAddressAsync(int id);
        Task<IEnumerable<Address>> GetAllAddressesForClientAsync(int clientId);
    }
}