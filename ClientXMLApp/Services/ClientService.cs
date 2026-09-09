using AutoMapper;
using ClientXMLApp.Data;
using ClientXMLApp.Models;
using ClientXMLApp.Services.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace ClientXMLApp.Services
{
    public class ClientService : IClientService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public ClientService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<ViewClientDto>> GetClientsAsync(ClientQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var normalized = query.Normalized();
            var totalCount = await _context.Clients.CountAsync(cancellationToken);

            var clients = await Sorted(ReadOnlyClients(), normalized.SortBy, normalized.SortAscending)
                .Skip((normalized.PageNumber - 1) * normalized.PageSize)
                .Take(normalized.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<ViewClientDto>(
                Map(clients),
                normalized.PageNumber,
                normalized.PageSize,
                totalCount);
        }

        public async IAsyncEnumerable<ViewClientDto> StreamAllClientsAsync(
            ClientSortingOptions sortBy = ClientSortingOptions.None,
            bool sortAscending = true,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var clients = Sorted(ExportClients(), sortBy, sortAscending).AsAsyncEnumerable();

            await foreach (var client in clients.WithCancellation(cancellationToken))
            {
                yield return _mapper.Map<ViewClientDto>(client);
            }
        }

        public async Task<ViewClientDto?> GetClientByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var client = await ReadOnlyClients().FirstOrDefaultAsync(c => c.ID == id, cancellationToken);

            return client == null ? null : _mapper.Map<ViewClientDto>(client);
        }

        public async Task<int> AddClientAsync(AddClientDto clientDto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(clientDto);

            var client = _mapper.Map<Client>(clientDto);
            _context.Clients.Add(client);
            await _context.SaveChangesAsync(cancellationToken);

            return client.ID;
        }

        public async Task<bool> UpdateClientAsync(UpdateClientDto clientDto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(clientDto);

            var client = await _context.Clients
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.ID == clientDto.ID, cancellationToken);

            if (client == null)
            {
                return false;
            }

            _mapper.Map(clientDto, client);

            // Clearing the tracked collection is what makes EF delete the removed rows.
            client.Addresses.Clear();
            foreach (var addressDto in clientDto.Addresses ?? new List<AddressDto>())
            {
                client.Addresses.Add(_mapper.Map<Address>(addressDto));
            }

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> DeleteClientAsync(int id, CancellationToken cancellationToken = default)
        {
            // Addresses go with the client through the declared cascade.
            var deleted = await _context.Clients
                .Where(c => c.ID == id)
                .ExecuteDeleteAsync(cancellationToken);

            return deleted > 0;
        }

        public async Task AddClientsAsync(IEnumerable<AddClientDto> clientDtos, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(clientDtos);

            var clients = clientDtos.Select(_mapper.Map<Client>).ToList();
            if (clients.Count == 0)
            {
                return;
            }

            _context.Clients.AddRange(clients);

            await _context.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<Client> ExportClients() => _context.Clients
            .AsNoTracking()
            .Include(c => c.Addresses)
            .AsSingleQuery();

        private IQueryable<Client> ReadOnlyClients() => _context.Clients
            .AsNoTracking()
            .Include(c => c.Addresses)
            .AsSplitQuery();

        // The key is the tiebreaker: a paged window needs a total order.
        private static IQueryable<Client> Sorted(IQueryable<Client> clients, ClientSortingOptions sortBy, bool ascending)
        {
            switch (sortBy)
            {
                case ClientSortingOptions.Name:
                    return ascending
                        ? clients.OrderBy(c => c.Name).ThenBy(c => c.ID)
                        : clients.OrderByDescending(c => c.Name).ThenByDescending(c => c.ID);
                case ClientSortingOptions.BirthDate:
                    return ascending
                        ? clients.OrderBy(c => c.BirthDate).ThenBy(c => c.ID)
                        : clients.OrderByDescending(c => c.BirthDate).ThenByDescending(c => c.ID);
                default:
                    return ascending
                        ? clients.OrderBy(c => c.ID)
                        : clients.OrderByDescending(c => c.ID);
            }
        }

        private IReadOnlyList<ViewClientDto> Map(IEnumerable<Client> clients) =>
            clients.Select(_mapper.Map<ViewClientDto>).ToList();
    }
}
