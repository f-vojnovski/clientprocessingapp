namespace ClientXMLApp.Services
{
    /// <summary>Its message is written for the uploader and is safe to display.</summary>
    public class ClientImportException : Exception
    {
        public ClientImportException(string message) : base(message)
        {
        }

        public ClientImportException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
