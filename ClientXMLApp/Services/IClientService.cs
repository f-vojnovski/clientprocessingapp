using ClientXMLApp.Services.DTOs;

namespace ClientXMLApp.Services
{
    public interface IClientService
    {
        Task<PagedResult<ViewClientDto>> GetClientsAsync(ClientQuery query, CancellationToken cancellationToken = default);

        IAsyncEnumerable<ViewClientDto> StreamAllClientsAsync(
            ClientSortingOptions sortBy = ClientSortingOptions.None,
            bool sortAscending = true,
            CancellationToken cancellationToken = default);

        Task<ViewClientDto?> GetClientByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <returns>The identity key assigned to the new client.</returns>
        Task<int> AddClientAsync(AddClientDto clientDto, CancellationToken cancellationToken = default);

        /// <returns>False when no client has that id.</returns>
        Task<bool> UpdateClientAsync(UpdateClientDto clientDto, CancellationToken cancellationToken = default);

        /// <returns>False when no client has that id.</returns>
        Task<bool> DeleteClientAsync(int id, CancellationToken cancellationToken = default);

        Task AddClientsAsync(IEnumerable<AddClientDto> clientDtos, CancellationToken cancellationToken = default);
    }
}
