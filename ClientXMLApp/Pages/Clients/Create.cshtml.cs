using ClientXMLApp.Services.DTOs;
using ClientXMLApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ClientXMLApp.Models;

namespace ClientXMLApp.Pages.Clients
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
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            Client.Addresses ??= new List<AddressDto>();

            // The collection binder stops at the first missing index, so a gap in the posted
            // names would drop every address after it without failing.
            if (PostedAddressCount() != Client.Addresses.Count)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Some addresses could not be read. Please re-enter them and submit again.");
            }

            if (!PostedBirthDateIsACalendarDate())
            {
                ModelState.AddModelError(
                    "Client.BirthDate",
                    $"Birthdate must be a date in {CalendarDate.Format} form.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _clientService.AddClientAsync(Client, cancellationToken);

            return RedirectToPage("/Clients/View");
        }

        private bool PostedBirthDateIsACalendarDate()
        {
            var posted = Request.Form["Client.BirthDate"].ToString();

            return string.IsNullOrWhiteSpace(posted) || CalendarDate.TryParse(posted, out _);
        }

        private int PostedAddressCount()
        {
            const string prefix = "Client.Addresses[";
            var indexes = new HashSet<int>();

            foreach (var key in Request.Form.Keys)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var end = key.IndexOf(']', prefix.Length);
                if (end > prefix.Length
                    && int.TryParse(key.AsSpan(prefix.Length, end - prefix.Length), out var index))
                {
                    indexes.Add(index);
                }
            }

            return indexes.Count;
        }

        public AddressType[] AddressTypes => (AddressType[])Enum.GetValues(typeof(AddressType));
    }
}
