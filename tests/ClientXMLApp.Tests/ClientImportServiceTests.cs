using ClientXMLApp.Models;
using ClientXMLApp.Services;
using ClientXMLApp.Tests.Fakes;
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
            await Assert.ThrowsAsync<ArgumentNullException>(() => CreateService().ImportClientsAsync(null!));
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
            const string withDtd = @"<?xml version=""1.0""?>
<!DOCTYPE Clients [<!ENTITY payload ""expanded"">]>
<Clients><Client><Name>&payload;</Name></Client></Clients>";
            using var stream = StreamOf(withDtd);

            await Assert.ThrowsAsync<ClientImportException>(() => CreateService().ImportClientsAsync(stream));

            Assert.Empty(_clientService.Batches);
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
        public async Task Holds_imported_records_to_the_same_rules_as_the_create_form()
        {
            // Two characters is below the minimum length on AddClientDto.Name.
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
        public async Task Imports_the_sample_file_that_ships_with_the_repository()
        {
            await using var file = File.OpenRead("client_import_example.xml");

            var imported = await CreateService().ImportClientsAsync(file);

            Assert.Equal(2, imported);
            Assert.All(_clientService.LastBatch, client => Assert.Equal(2, client.Addresses.Count));
        }
    }
}
