using ClientXMLApp.Models;
using ClientXMLApp.Services.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace ClientXMLApp.Services
{
    public class ClientImportService : IClientImportService
    {
        private const int MaxReportedFailures = 10;

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

            var failures = new FailureLog();
            var clientDtos = new List<AddClientDto>(document.Clients.Count);

            for (var i = 0; i < document.Clients.Count; i++)
            {
                clientDtos.Add(ToDto(document.Clients[i], i + 1, failures));
            }

            Validate(clientDtos, failures);

            if (failures.Total > 0)
            {
                throw new ClientImportException(
                    "The file was read but some records are not valid, so nothing was imported. "
                    + failures.Describe());
            }

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

        private static AddClientDto ToDto(XmlClient xmlClient, int position, FailureLog failures)
        {
            return new AddClientDto
            {
                Name = xmlClient.Name,
                BirthDate = ParseBirthDate(xmlClient.BirthDate, position, failures),
                Addresses = (xmlClient.Addresses ?? new List<XmlAddress>())
                    .Select(xmlAddress => ToDto(xmlAddress, position, failures))
                    .ToList()
            };
        }

        private static AddressDto ToDto(XmlAddress xmlAddress, int position, FailureLog failures)
        {
            if (xmlAddress.UnexpectedContent?.Length > 0)
            {
                failures.Add($"Client {position}: an address contains markup, which is not allowed.");
            }

            return new AddressDto
            {
                AddressText = xmlAddress.AddressText,
                Type = ParseAddressType(xmlAddress.Type, position, failures)
            };
        }

        private static AddressType ParseAddressType(string? value, int position, FailureLog failures)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                failures.Add($"Client {position}: an address is missing its Type attribute.");

                return AddressType.Unknown;
            }

            if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                // An out-of-range number is left to the annotation on AddressDto.
                return (AddressType)parsed;
            }

            failures.Add($"Client {position}: an address Type of '{value}' is not a whole number.");

            return AddressType.Unknown;
        }

        /// <summary>
        /// Only a plain calendar date is accepted. A value carrying a time or an offset would
        /// otherwise be shifted into the server's local time and land on a different day.
        /// </summary>
        private static DateTime? ParseBirthDate(string? value, int position, FailureLog failures)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParseExact(
                    value.Trim(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                return parsed;
            }

            failures.Add($"Client {position}: BirthDate must be a date in yyyy-MM-dd form.");

            return null;
        }

        private static void Validate(IReadOnlyList<AddClientDto> clientDtos, FailureLog failures)
        {
            for (var i = 0; i < clientDtos.Count; i++)
            {
                var clientDto = clientDtos[i];
                var position = i + 1;

                var errors = ValidationErrors(clientDto)
                    .Concat(clientDto.Addresses.SelectMany(ValidationErrors));

                foreach (var error in errors)
                {
                    failures.Add($"Client {position}: {error}");
                }
            }
        }

        private sealed class FailureLog
        {
            private readonly List<string> _reported = new List<string>();

            public int Total { get; private set; }

            public void Add(string failure)
            {
                Total++;

                if (_reported.Count < MaxReportedFailures)
                {
                    _reported.Add(failure);
                }
            }

            public string Describe()
            {
                var detail = string.Join(" ", _reported);

                return Total > _reported.Count
                    ? detail + $" (and {Total - _reported.Count} more problems)"
                    : detail;
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
