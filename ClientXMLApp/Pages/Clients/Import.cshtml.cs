using ClientXMLApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientXMLApp.Pages.Clients
{
    public class ImportModel : PageModel
    {
        private const long MaxUploadBytes = 10 * 1024 * 1024;

        private readonly IClientImportService _importService;
        private readonly ILogger<ImportModel> _logger;

        public ImportModel(IClientImportService importService, ILogger<ImportModel> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        [BindProperty]
        public IFormFile? XmlFile { get; set; }

        public int ImportedCount { get; private set; }
        public string? ErrorMessage { get; private set; }

        public bool ImportSucceeded => ImportedCount > 0;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (XmlFile == null || XmlFile.Length == 0)
            {
                ErrorMessage = "Choose an XML file to import.";
                return Page();
            }

            if (XmlFile.Length > MaxUploadBytes)
            {
                ErrorMessage = $"That file is larger than the {MaxUploadBytes / (1024 * 1024)} MB limit.";
                return Page();
            }

            try
            {
                await using var stream = XmlFile.OpenReadStream();
                ImportedCount = await _importService.ImportClientsAsync(stream, cancellationToken);
            }
            catch (ClientImportException ex)
            {
                _logger.LogWarning(ex, "Rejected client import from {FileName}", XmlFile.FileName);
                ErrorMessage = ex.Message;
            }

            return Page();
        }
    }
}
