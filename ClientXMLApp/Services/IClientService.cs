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

        /// <returns>The identity key assigned to the new client.</returns>
        Task<int> AddClientAsync(AddClientDto clientDto, CancellationToken cancellationToken = default);

        Task AddClientsAsync(IEnumerable<AddClientDto> clientDtos, CancellationToken cancellationToken = default);
    }
}
