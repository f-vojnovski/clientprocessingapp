using ClientXMLApp.Models;
using AutoMapper;
using ClientXMLApp.Services.DTOs;

namespace ClientXMLApp.Services
{
    public class ClientService : IClientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ClientService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ViewClientDto>> GetAllClientsAsync(ClientSortingOptions sortBy = ClientSortingOptions.None, bool sortAscending = true)
        {
            var clients = await _unitOfWork.Clients.GetAllClientsAsync();
            var clientDtos = clients.Select(client => _mapper.Map<ViewClientDto>(client));

            switch (sortBy)
            {
                case ClientSortingOptions.Name:
                    clientDtos = sortAscending
                        ? clientDtos.OrderBy(c => c.Name)
                        : clientDtos.OrderByDescending(c => c.Name);
                    break;
                case ClientSortingOptions.BirthDate:
                    clientDtos = sortAscending
                        ? clientDtos.OrderBy(c => c.BirthDate)
                        : clientDtos.OrderByDescending(c => c.BirthDate);
                    break;
            }

            return clientDtos;
        }

        public async Task<ViewClientDto> GetClientByIdAsync(int id)
        {
            var client = await _unitOfWork.Clients.GetClientByIdAsync(id);
            return _mapper.Map<ViewClientDto>(client);
        }

        public async Task AddClientAsync(AddClientDto clientDto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var client = _mapper.Map<Client>(clientDto);
                await _unitOfWork.Clients.AddClientAsync(client);
                await _unitOfWork.CompleteAsync();

                foreach (var addressDto in clientDto.Addresses)
                {
                    var address = _mapper.Map<Address>(addressDto);
                    address.ClientID = client.ID;
                    await _unitOfWork.Addresses.AddAddressAsync(address);
                }

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task UpdateClientAsync(UpdateClientDto clientDto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var client = await _unitOfWork.Clients.GetClientByIdAsync(clientDto.ID);
                if (client != null)
                {
                    _mapper.Map(clientDto, client);
                    await _unitOfWork.Clients.UpdateClientAsync(client);
                    await _unitOfWork.CompleteAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task DeleteClientAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var client = await _unitOfWork.Clients.GetClientByIdAsync(id);
                if (client != null)
                {
                    var addresses = await _unitOfWork.Addresses.GetAllAddressesForClientAsync(id);
                    foreach (var address in addresses)
                    {
                        await _unitOfWork.Addresses.DeleteAddressAsync(address.ID);
                    }

                    await _unitOfWork.Clients.DeleteClientAsync(id);
                    await _unitOfWork.CompleteAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task AddClientsAsync(IEnumerable<AddClientDto> clientDtos)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var clientDto in clientDtos)
                {
                    var client = _mapper.Map<Client>(clientDto);
                    await _unitOfWork.Clients.AddClientAsync(client);
                    await _unitOfWork.CompleteAsync();

                    foreach (var addressDto in clientDto.Addresses)
                    {
                        var address = _mapper.Map<Address>(addressDto);
                        address.ClientID = client.ID;
                        await _unitOfWork.Addresses.AddAddressAsync(address);
                    }
                }

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
