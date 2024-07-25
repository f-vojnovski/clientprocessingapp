using ClientXMLApp.Services.DTOs;
using ClientXMLApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public async Task OnGetAsync()
        {
            Clients = (await _clientService.GetAllClientsAsync()).ToList();
        }
    }
}
