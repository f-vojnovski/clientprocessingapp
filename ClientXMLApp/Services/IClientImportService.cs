namespace ClientXMLApp.Services
{
    public interface IClientImportService
    {
        Task ImportClientsAsync(string xmlFilePath);
    }
}
