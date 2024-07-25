using System.Collections.Generic;
using System.Threading.Tasks;
using ClientXMLApp.Models;
using ClientXMLApp.Data;

namespace ClientXMLApp.Services
{
    public class ClientService : IClientService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClientService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync()
        {
            return await _unitOfWork.Clients.GetAllClientsAsync();
        }

        public async Task<Client> GetClientByIdAsync(string id)
        {
            return await _unitOfWork.Clients.GetClientByIdAsync(id);
        }

        public async Task AddClientAsync(Client client)
        {
            await _unitOfWork.Clients.AddClientAsync(client);
            await _unitOfWork.CompleteAsync();
        }

        public async Task UpdateClientAsync(Client client)
        {
            await _unitOfWork.Clients.UpdateClientAsync(client);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteClientAsync(string id)
        {
            await _unitOfWork.Clients.DeleteClientAsync(id);
            await _unitOfWork.CompleteAsync();
        }
    }
}
