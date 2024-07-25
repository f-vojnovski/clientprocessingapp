using ClientXMLApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientXMLApp.Pages.Clients
{
    public class ImportModel : PageModel
    {
        private readonly IClientImportService _importService;

        public ImportModel(IClientImportService importService)
        {
            _importService = importService;
        }

        [BindProperty]
        public IFormFile XmlFile { get; set; }

        public bool ImportSuccess { get; set; }
        public string ImportError { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (XmlFile == null || XmlFile.Length == 0)
            {
                ImportError = "Please select a valid XML file.";
                return Page();
            }

            try
            {
                var filePath = Path.GetTempFileName();

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await XmlFile.CopyToAsync(stream);
                }

                await _importService.ImportClientsAsync(filePath);
                System.IO.File.Delete(filePath);

                ImportSuccess = true;
            }
            catch (Exception ex)
            {
                ImportError = $"An error occurred while importing clients: {ex.Message}";
            }

            return Page();
        }
    }
}
