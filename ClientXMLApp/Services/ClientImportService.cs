using ClientXMLApp.Models;
using ClientXMLApp.Services.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Xml.Serialization;

namespace ClientXMLApp.Services
{
    public class ClientImportService : IClientImportService
    {
        private const string MalformedMessage =
            "The file could not be read as a client XML document. Check that it is well-formed XML with a <Clients> root element.";

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(XmlClientList));

        // Untrusted input: no DTD, no external entity resolution.
        private static readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            CloseInput = false
        };

        private readonly IClientService _clientService;

        public ClientImportService(IClientService clientService)
        {
            _clientService = clientService;
        }

        public async Task<int> ImportClientsAsync(Stream xmlStream, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xmlStream);

            var document = Deserialize(xmlStream);

            if (document.Clients == null || document.Clients.Count == 0)
            {
                throw new ClientImportException("The file contains no client records.");
            }

            var clientDtos = document.Clients.Select(ToDto).ToList();
            Validate(clientDtos);

            await _clientService.AddClientsAsync(clientDtos, cancellationToken);

            return clientDtos.Count;
        }

        private static XmlClientList Deserialize(Stream xmlStream)
        {
            try
            {
                using var reader = XmlReader.Create(xmlStream, ReaderSettings);
                var document = Serializer.Deserialize(reader) as XmlClientList;

                return document ?? throw new ClientImportException(MalformedMessage);
            }
            catch (InvalidOperationException ex)
            {
                // XmlSerializer wraps every reader-level failure, XmlException included.
                throw new ClientImportException(MalformedMessage, ex);
            }
            catch (XmlException ex)
            {
                throw new ClientImportException(MalformedMessage, ex);
            }
        }

        private static AddClientDto ToDto(XmlClient xmlClient)
        {
            return new AddClientDto
            {
                Name = xmlClient.Name,
                BirthDate = xmlClient.BirthDate,
                Addresses = (xmlClient.Addresses ?? new List<XmlAddress>())
                    .Select(xmlAddress => new AddressDto
                    {
                        AddressText = xmlAddress.AddressText,
                        Type = (AddressType)xmlAddress.Type
                    })
                    .ToList()
            };
        }

        private static void Validate(IReadOnlyList<AddClientDto> clientDtos)
        {
            var failures = new List<string>();

            for (var i = 0; i < clientDtos.Count; i++)
            {
                var clientDto = clientDtos[i];
                var position = i + 1;

                foreach (var error in ValidationErrors(clientDto))
                {
                    failures.Add($"Client {position}: {error}");
                }

                if (clientDto.Addresses.Count == 0)
                {
                    failures.Add($"Client {position}: at least one address is required.");
                }

                foreach (var error in clientDto.Addresses.SelectMany(ValidationErrors))
                {
                    failures.Add($"Client {position}: {error}");
                }
            }

            if (failures.Count > 0)
            {
                throw new ClientImportException(
                    "The file was read but some records are not valid, so nothing was imported. "
                    + string.Join(" ", failures));
            }
        }

        // TryValidateObject does not recurse into collections, so addresses are validated above.
        private static IEnumerable<string> ValidationErrors(object instance)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);

            return results.Select(result => result.ErrorMessage ?? "invalid value.");
        }
    }
}
