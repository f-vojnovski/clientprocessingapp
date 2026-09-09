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
            Response.ContentType = "application/json";
            Response.Headers.ContentDisposition = "attachment; filename=clients.json";

            var clients = _clientService.StreamAllClientsAsync(SortBy, SortAscending, cancellationToken);
            await JsonSerializer.SerializeAsync(Response.Body, clients, ExportOptions, cancellationToken);

            return new EmptyResult();
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
