using ClientXMLApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientXMLApp.Pages.Clients
{
    public class ImportModel : PageModel
    {
        private const long MaxUploadBytes = 10 * 1024 * 1024;
        private const string ImportedCountKey = "ImportedCount";
        private const string ErrorMessageKey = "ImportError";

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
            if (TempData[ImportedCountKey] is int importedCount)
            {
                ImportedCount = importedCount;
            }

            ErrorMessage = TempData[ErrorMessageKey] as string;
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (XmlFile == null || XmlFile.Length == 0)
            {
                return Rejected("Choose an XML file to import.");
            }

            if (XmlFile.Length > MaxUploadBytes)
            {
                return Rejected($"That file is larger than the {MaxUploadBytes / (1024 * 1024)} MB limit.");
            }

            try
            {
                await using var stream = XmlFile.OpenReadStream();
                TempData[ImportedCountKey] = await _importService.ImportClientsAsync(stream, cancellationToken);
            }
            catch (ClientImportException ex)
            {
                _logger.LogWarning(ex, "Rejected client import from {FileName}", XmlFile.FileName);
                return Rejected(ex.Message);
            }

            return RedirectToPage();
        }

        private IActionResult Rejected(string message)
        {
            TempData[ErrorMessageKey] = message;

            return RedirectToPage();
        }
    }
}
