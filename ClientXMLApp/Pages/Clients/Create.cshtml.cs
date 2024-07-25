using ClientXMLApp.Services.DTOs;
using ClientXMLApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ClientXMLApp.Models;

namespace ClientXMLApp.Pages.Client
{
    public class CreateModel : PageModel
    {
        private readonly IClientService _clientService;

        [BindProperty]
        public AddClientDto Client { get; set; }

        public CreateModel(IClientService clientService)
        {
            _clientService = clientService;
            Client = new AddClientDto();
            Client.Addresses = new List<AddressDto>();
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _clientService.AddClientAsync(Client);

            return RedirectToPage("Clients");
        }

        public AddressType[] AddressTypes => (AddressType[])Enum.GetValues(typeof(AddressType));
    }
}
