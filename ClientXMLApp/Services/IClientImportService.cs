namespace ClientXMLApp.Services
{
    public interface IClientImportService
    {
        /// <summary>The caller owns the stream.</summary>
        /// <returns>The number of clients imported.</returns>
        /// <exception cref="ClientImportException">Malformed, empty, or fails validation.</exception>
        Task<int> ImportClientsAsync(Stream xmlStream, CancellationToken cancellationToken = default);
    }
}
