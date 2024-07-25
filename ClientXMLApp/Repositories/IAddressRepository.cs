using ClientXMLApp.Models;


namespace ClientXMLApp.Repositories
{
    public interface IAddressRepository
    {
        Task<IEnumerable<Address>> GetAllAddressesAsync();
        Task<Address> GetAddressByIdAsync(AddressType type, string clientId);
        Task AddAddressAsync(Address address);
        Task UpdateAddressAsync(Address address);
        Task DeleteAddressAsync(AddressType type, string clientId);
    }
}