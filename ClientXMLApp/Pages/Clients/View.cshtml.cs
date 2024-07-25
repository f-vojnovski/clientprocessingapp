using ClientXMLApp.Services.DTOs;
using ClientXMLApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace ClientXMLApp.Pages.Clients
{
    public class ViewModel : PageModel
    {
        private readonly IClientService _clientService;

        public ViewModel(IClientService clientService)
        {
            _clientService = clientService;
        }

        public IList<ViewClientDto> Clients { get; set; }

        [BindProperty(SupportsGet = true)]
        public ClientSortingOptions SortBy { get; set; } = ClientSortingOptions.None;

        [BindProperty(SupportsGet = true)]
        public bool SortAscending { get; set; } = true;

        public async Task OnGetAsync()
        {
            Clients = (await _clientService.GetAllClientsAsync(SortBy, SortAscending)).ToList();
        }
    }
}
