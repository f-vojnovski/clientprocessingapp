using Microsoft.AspNetCore.Mvc.RazorPages;
using ClientXMLApp.Models;
using ClientXMLApp.Services;

namespace ClientXMLApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IClientService _clientService;

        public IndexModel(ILogger<IndexModel> logger, IClientService clientService)
        {
            _logger = logger;
            _clientService = clientService;
        }

        public IEnumerable<Client> Clients { get; private set; }

        public async Task OnGetAsync()
        {
            Clients = await _clientService.GetAllClientsAsync();
        }
    }
}