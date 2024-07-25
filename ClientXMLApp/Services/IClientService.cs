using System.Collections.Generic;
using System.Threading.Tasks;
using ClientXMLApp.Models;

namespace ClientXMLApp.Services
{
    public interface IClientService
    {
        Task<IEnumerable<Client>> GetAllClientsAsync();
        Task<Client> GetClientByIdAsync(string id);
        Task AddClientAsync(Client client);
        Task UpdateClientAsync(Client client);
        Task DeleteClientAsync(string id);
    }
}