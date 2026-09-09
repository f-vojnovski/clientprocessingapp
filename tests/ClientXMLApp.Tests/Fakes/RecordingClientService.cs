using ClientXMLApp.Services;
using ClientXMLApp.Services.DTOs;

namespace ClientXMLApp.Tests.Fakes
{
    // Records every batch handed to it, so a test can assert a rejected file reached it never.
    public class RecordingClientService : IClientService
    {
        public List<IReadOnlyList<AddClientDto>> Batches { get; } = new List<IReadOnlyList<AddClientDto>>();

        public IReadOnlyList<AddClientDto> LastBatch => Batches.Count == 0
            ? Array.Empty<AddClientDto>()
            : Batches[Batches.Count - 1];

        public Task AddClientsAsync(IEnumerable<AddClientDto> clientDtos, CancellationToken cancellationToken = default)
        {
            Batches.Add(clientDtos.ToList());
            return Task.CompletedTask;
        }

        public Task<PagedResult<ViewClientDto>> GetClientsAsync(ClientQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<ViewClientDto> StreamAllClientsAsync(
            ClientSortingOptions sortBy = ClientSortingOptions.None,
            bool sortAscending = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> AddClientAsync(AddClientDto clientDto, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
