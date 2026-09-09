using ClientXMLApp.Models;
using ClientXMLApp.Services;
using ClientXMLApp.Tests.Fakes;
using System.Globalization;
using System.Text;

namespace ClientXMLApp.Tests
{
    public class ClientImportServiceTests
    {
        private const string TwoClients = @"<Clients>
    <Client ID=""12345"">
        <Name>Ime1</Name>
        <Addresses>
            <Address Type=""1"">Home address</Address>
            <Address Type=""2"">Weekend address</Address>
        </Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
    <Client ID=""54321"">
        <Name>Ime2</Name>
        <Addresses>
            <Address Type=""1"">Home address</Address>
        </Addresses>
        <BirthDate>2003-09-01</BirthDate>
    </Client>
</Clients>";

        private readonly RecordingClientService _clientService = new RecordingClientService();

        private ClientImportService CreateService() => new ClientImportService(_clientService);

        private static Stream StreamOf(string xml) => new MemoryStream(Encoding.UTF8.GetBytes(xml));

        [Fact]
        public async Task Reads_clients_from_a_stream()
        {
            using var xml = StreamOf(TwoClients);

            var imported = await CreateService().ImportClientsAsync(xml);

            Assert.Equal(2, imported);
            Assert.Equal(new[] { "Ime1", "Ime2" }, _clientService.LastBatch.Select(c => c.Name));
            Assert.Equal(new DateTime(2001, 9, 1), _clientService.LastBatch[0].BirthDate);
        }

        [Fact]
        public async Task Maps_the_type_attribute_and_the_element_body_of_each_address()
        {
            using var xml = StreamOf(TwoClients);

            await CreateService().ImportClientsAsync(xml);

            var addresses = _clientService.LastBatch[0].Addresses;
            Assert.Equal(2, addresses.Count);
            Assert.Equal(AddressType.Home, addresses[0].Type);
            Assert.Equal("Home address", addresses[0].AddressText);
            Assert.Equal(AddressType.Public, addresses[1].Type);
            Assert.Equal("Weekend address", addresses[1].AddressText);
        }

        [Fact]
        public async Task Leaves_the_stream_open_for_the_caller_to_dispose()
        {
            using var xml = StreamOf(TwoClients);

            await CreateService().ImportClientsAsync(xml);

            Assert.True(xml.CanRead);
        }

        [Fact]
        public async Task Rejects_a_null_stream()
        {
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(
                () => CreateService().ImportClientsAsync(null!));

            // Without the guard the reader throws this too, but names its own parameter.
            Assert.Equal("xmlStream", ex.ParamName);
        }

        [Theory]
        [InlineData("<Clients><Client><Name>Ime1</Name>")]
        [InlineData("not xml at all")]
        [InlineData("<Customers><Customer /></Customers>")]
        [InlineData("<Clients><Client><BirthDate>the third of never</BirthDate></Client></Clients>")]
        public async Task Turns_a_malformed_document_into_a_rejection(string xml)
        {
            using var stream = StreamOf(xml);

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Rejects_a_document_that_declares_a_dtd()
        {
            // Everything but the DOCTYPE is valid, so only the reader settings decide the
            // outcome. An unhardened reader expands the entity and the import succeeds.
            const string withDtd = @"<?xml version=""1.0""?>
<!DOCTYPE Clients [<!ENTITY payload ""Ime1"">]>
<Clients>
    <Client><Name>&payload;</Name>
        <Addresses><Address Type=""1"">Home address</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
</Clients>";
            using var stream = StreamOf(withDtd);

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("could not be read", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Rejects_a_document_that_pulls_in_an_external_entity()
        {
            var secret = Path.Combine(Path.GetTempPath(), $"xxe-{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(secret, "LeakedFileContents");
            try
            {
                var withExternalEntity = $@"<?xml version=""1.0""?>
<!DOCTYPE Clients [<!ENTITY payload SYSTEM ""file:///{secret.Replace("\\", "/")}"">]>
<Clients>
    <Client><Name>&payload;</Name>
        <Addresses><Address Type=""1"">Home address</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
</Clients>";
                using var stream = StreamOf(withExternalEntity);

                var ex = await Assert.ThrowsAsync<ClientImportException>(
                    () => CreateService().ImportClientsAsync(stream));

                Assert.Contains("could not be read", ex.Message);
                Assert.Empty(_clientService.Batches);
                Assert.DoesNotContain("LeakedFileContents", ex.Message);
            }
            finally
            {
                File.Delete(secret);
            }
        }

        [Fact]
        public async Task Rejects_a_document_with_no_client_records()
        {
            using var stream = StreamOf("<Clients></Clients>");

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("no client records", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Rejects_a_name_shorter_than_the_minimum()
        {
            const string shortName = @"<Clients>
    <Client><Name>Im</Name>
        <Addresses><Address Type=""1"">Home address</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
</Clients>";
            using var stream = StreamOf(shortName);

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("3 characters", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Rejects_the_whole_file_when_a_single_record_is_invalid()
        {
            const string oneBadRecord = @"<Clients>
    <Client><Name>Ime1</Name>
        <Addresses><Address Type=""1"">Home address</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
    <Client><Name>Ime2</Name>
        <Addresses><Address Type=""1"">bad</Address></Addresses>
        <BirthDate>2003-09-01</BirthDate>
    </Client>
</Clients>";
            using var stream = StreamOf(oneBadRecord);

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("Client 2", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Reports_a_bounded_number_of_problems_and_counts_the_rest()
        {
            var many = new StringBuilder("<Clients>");
            for (var i = 0; i < 500; i++)
            {
                many.Append("<Client><Name>x</Name></Client>");
            }
            many.Append("</Clients>");
            using var stream = StreamOf(many.ToString());

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("more problems)", ex.Message);
            Assert.True(ex.Message.Length < 2000, $"message grew to {ex.Message.Length} characters");
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task States_the_missing_address_rule_once()
        {
            using var stream = StreamOf(@"<Clients>
    <Client><Name>Ime1</Name><BirthDate>2001-09-01</BirthDate></Client>
</Clients>");

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            var occurrences = ex.Message.Split("address is required").Length - 1;
            Assert.Equal(1, occurrences);
        }

        [Fact]
        public async Task Rejects_a_client_that_carries_no_address()
        {
            const string noAddresses = @"<Clients>
    <Client><Name>Ime1</Name><BirthDate>2001-09-01</BirthDate></Client>
</Clients>";
            using var stream = StreamOf(noAddresses);

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("address", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Rejects_a_name_longer_than_the_column_allows()
        {
            var tooLong = new string('a', 201);
            var xml = $@"<Clients>
    <Client><Name>{tooLong}</Name>
        <Addresses><Address Type=""1"">Home address</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
</Clients>";
            using var stream = StreamOf(xml);

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("at most 200 characters", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Rejects_a_client_with_no_birth_date()
        {
            using var stream = StreamOf(@"<Clients>
    <Client><Name>Ime1</Name>
        <Addresses><Address Type=""1"">Home address</Address></Addresses>
    </Client>
</Clients>");

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("Birthdate is required", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Theory]
        [InlineData("3")]
        [InlineData("9")]
        [InlineData("-4")]
        public async Task Rejects_an_address_type_outside_the_enum(string type)
        {
            using var stream = StreamOf($@"<Clients>
    <Client><Name>Ime1</Name>
        <Addresses><Address Type=""{type}"">Home address</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
</Clients>");

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("Address type is not a known value", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Rejects_an_address_whose_body_contains_markup()
        {
            using var stream = StreamOf(@"<Clients>
    <Client><Name>Ime1</Name>
        <Addresses><Address Type=""1"">Home <b>address</b> here</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
</Clients>");

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("markup", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Theory]
        [InlineData("2001-09-02T01:00:00+05:00")]
        [InlineData("2001-09-02T01:00:00Z")]
        [InlineData("02/09/2001")]
        [InlineData("not a date")]
        public async Task Rejects_a_birth_date_that_is_not_a_plain_calendar_date(string value)
        {
            using var stream = StreamOf($@"<Clients>
    <Client><Name>Ime1</Name>
        <Addresses><Address Type=""1"">Home address</Address></Addresses>
        <BirthDate>{value}</BirthDate>
    </Client>
</Clients>");

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("yyyy-MM-dd", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Keeps_the_date_it_was_given_regardless_of_the_server_timezone()
        {
            using var stream = StreamOf(@"<Clients>
    <Client><Name>Ime1</Name>
        <Addresses><Address Type=""1"">Home address</Address></Addresses>
        <BirthDate>2001-09-02</BirthDate>
    </Client>
</Clients>");

            await CreateService().ImportClientsAsync(stream);

            Assert.Equal(new DateTime(2001, 9, 2), _clientService.LastBatch[0].BirthDate);
        }

        [Fact]
        public async Task Rejects_an_address_with_no_type_attribute()
        {
            using var stream = StreamOf(@"<Clients>
    <Client><Name>Ime1</Name>
        <Addresses><Address>Home address</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
</Clients>");

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("missing its Type", ex.Message);
            Assert.Empty(_clientService.Batches);
        }

        [Fact]
        public async Task Reports_a_non_numeric_address_type_as_such()
        {
            using var stream = StreamOf(@"<Clients>
    <Client><Name>Ime1</Name>
        <Addresses><Address Type=""abc"">Home address</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
</Clients>");

            var ex = await Assert.ThrowsAsync<ClientImportException>(
                () => CreateService().ImportClientsAsync(stream));

            Assert.Contains("not a whole number", ex.Message);
            Assert.DoesNotContain("could not be read", ex.Message);
        }

        [Fact]
        public async Task Imports_a_document_declared_in_a_legacy_code_page()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            const string xml = @"<?xml version=""1.0"" encoding=""windows-1250""?>
<Clients>
    <Client><Name>Zdravko Šimić</Name>
        <Addresses><Address Type=""1"">Ulica Šenoina 12</Address></Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
</Clients>";
            using var stream = new MemoryStream(Encoding.GetEncoding("windows-1250").GetBytes(xml));

            var imported = await CreateService().ImportClientsAsync(stream);

            Assert.Equal(1, imported);
            Assert.Equal("Zdravko Šimić", _clientService.LastBatch[0].Name);
        }

        [Theory]
        [InlineData("th-TH")]
        [InlineData("ar-SA")]
        [InlineData("de-DE")]
        public async Task Reads_the_same_date_under_any_server_culture(string culture)
        {
            var original = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            try
            {
                using var stream = StreamOf(@"<Clients>
    <Client><Name>Ime1</Name>
        <Addresses><Address Type=""1"">Home address</Address></Addresses>
        <BirthDate>2001-09-02</BirthDate>
    </Client>
</Clients>");

                await CreateService().ImportClientsAsync(stream);

                Assert.Equal(new DateTime(2001, 9, 2), _clientService.LastBatch[0].BirthDate);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Fact]
        public async Task Imports_the_sample_file_that_ships_with_the_repository()
        {
            await using var file = File.OpenRead("client_import_example.xml");

            var imported = await CreateService().ImportClientsAsync(file);

            Assert.Equal(2, imported);
            Assert.All(_clientService.LastBatch, client => Assert.Equal(2, client.Addresses.Count));
        }
    }
}
