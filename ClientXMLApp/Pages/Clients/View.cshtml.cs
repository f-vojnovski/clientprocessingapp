using ClientXMLApp.Services;
using ClientXMLApp.Services.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClientXMLApp.Pages.Clients
{
    public class ViewModel : PageModel
    {
        private static readonly JsonSerializerOptions ExportOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly IClientService _clientService;

        public ViewModel(IClientService clientService)
        {
            _clientService = clientService;
        }

        public PagedResult<ViewClientDto> Clients { get; private set; } =
            new PagedResult<ViewClientDto>(Array.Empty<ViewClientDto>(), 1, ClientQuery.DefaultPageSize, 0);

        [BindProperty(SupportsGet = true)]
        public ClientSortingOptions SortBy { get; set; } = ClientSortingOptions.None;

        [BindProperty(SupportsGet = true)]
        public bool SortAscending { get; set; } = true;

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = ClientQuery.DefaultPageSize;

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            Clients = await _clientService.GetClientsAsync(CurrentQuery(), cancellationToken);
        }

        public async Task<IActionResult> OnGetExportAsync(CancellationToken cancellationToken)
        {
            var clients = await _clientService.GetAllClientsAsync(SortBy, SortAscending, cancellationToken);
            var json = JsonSerializer.SerializeToUtf8Bytes(clients, ExportOptions);

            return File(json, "application/json", "clients.json");
        }

        public ClientQuery CurrentQuery() => new ClientQuery
        {
            SortBy = SortBy,
            SortAscending = SortAscending,
            PageNumber = PageNumber,
            PageSize = PageSize
        };
    }
}
