using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientXMLApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
        }

        public async Task OnGetAsync()
        {
        }
    }
}