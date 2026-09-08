# ClientXMLApp

An ASP.NET Core Razor Pages application for importing client records from XML and managing
them against SQL Server. Clients come in through a web form or as an uploaded XML file with
nested address lists. Both paths validate the input and write it through a repository and
unit-of-work layer inside a single transaction.

The XML import is the part worth reading. It takes a `Clients -> Client -> Addresses -> Address`
document where identifiers and address types are attributes and address text is the element
body, and maps it onto plain C# classes with `XmlSerializer` and serialization attributes.

## Features

- Upload an XML file and import every client and address it contains ([Import](ClientXMLApp/Pages/Clients/Import.cshtml))
- Add a single client with a dynamic, add-and-remove address table ([Create](ClientXMLApp/Pages/Clients/Create.cshtml))
- List clients with their addresses, sortable by name or birth date in either direction ([View](ClientXMLApp/Pages/Clients/View.cshtml))
- Export the current client list to a `clients.json` download
- Server-side and client-side validation on client and address input
- Transactional writes with rollback, so a malformed file leaves no partial data behind

## Stack

.NET 8, ASP.NET Core Razor Pages, Entity Framework Core 8.0.7 with the SQL Server provider,
AutoMapper 13.0.1, Bootstrap 5 and jQuery validation for the front end.

## Architecture

Four layers, wired together by constructor injection and registered in
[Program.cs](ClientXMLApp/Program.cs):

```
Razor Pages  ->  Services  ->  Unit of Work  ->  Repositories  ->  EF Core / SQL Server
```

### Pages

Page models depend on service interfaces only. [Import.cshtml.cs](ClientXMLApp/Pages/Clients/Import.cshtml.cs)
depends on `IClientImportService`; [Create.cshtml.cs](ClientXMLApp/Pages/Clients/Create.cshtml.cs)
and [View.cshtml.cs](ClientXMLApp/Pages/Clients/View.cshtml.cs) depend on `IClientService`.

### Services

[IClientService](ClientXMLApp/Services/IClientService.cs) exposes the client operations:
`GetAllClientsAsync`, `GetClientByIdAsync`, `AddClientAsync`, `UpdateClientAsync`,
`DeleteClientAsync`, and the bulk `AddClientsAsync` the importer uses.

[ClientService](ClientXMLApp/Services/ClientService.cs) opens a transaction for every write,
rolls back and rethrows on failure, and handles the identity-key ordering that address inserts
require: the client is saved first so SQL Server assigns its `ID`, then each address is written
with that `ID` as its foreign key. Sorting is applied to the mapped DTOs in
`GetAllClientsAsync` based on a `ClientSortingOptions` value and an ascending flag.

[ClientImportService](ClientXMLApp/Services/ClientImportService.cs) owns XML deserialization
and delegates persistence to `IClientService`, so it holds no repository or transaction code
of its own.

### Unit of work

[IUnitOfWork](ClientXMLApp/Data/IUnitOfWork.cs) hands out the two repositories and controls
the transaction boundary: `CompleteAsync`, `BeginTransactionAsync`, `CommitTransactionAsync`,
`RollbackTransactionAsync`. [UnitOfWork](ClientXMLApp/Data/UnitOfWork.cs) constructs both
repositories over one shared `AppDbContext`, so a multi-client import commits or fails as one
atomic operation.

### Repositories

[IClientRepository](ClientXMLApp/Repositories/IClientRepository.cs) and
[IAddressRepository](ClientXMLApp/Repositories/IAddressRepository.cs) keep LINQ and EF Core
concerns out of the service layer. Client reads eagerly `Include` their addresses;
`IAddressRepository` adds `GetAllAddressesForClientAsync`, which the delete path uses to clear
a client's addresses before the client itself. Repository methods stage changes only, leaving
`SaveChangesAsync` to the unit of work.

### DTOs and mapping

Entities never reach a page. Six files under [Services/DTOs](ClientXMLApp/Services/DTOs)
define the boundary types: `AddClientDto`, `UpdateClientDto`, `ViewClientDto`, `AddressDto`,
the `ClientSortingOptions` enum, and `ClientXMLImportData.cs`, which holds the three
serialization types `XmlClientList`, `XmlClient` and `XmlAddress`.

[MappingProfile](ClientXMLApp/Services/MappingProfile.cs) registers the entity-to-DTO
conversions with AutoMapper, including the nested address collections, and is picked up by
`AddAutoMapper(typeof(MappingProfile))`.

### Dependency injection

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("local")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IClientImportService, ClientImportService>();

builder.Services.AddAutoMapper(typeof(MappingProfile));
```

## XML import

[client_import_example.xml](client_import_example.xml) is a working sample and imports as-is:

```xml
<Clients>
    <Client ID="12345">
        <Name>Ime1</Name>
        <Addresses>
            <Address Type="1">Home address</Address>
            <Address Type="2">Weekend address</Address>
        </Addresses>
        <BirthDate>2001-09-01</BirthDate>
    </Client>
    <Client ID="54321">
        <Name>Ime2</Name>
        <Addresses>
            <Address Type="1">Home address</Address>
            <Address Type="2">Weekend address</Address>
        </Addresses>
        <BirthDate>2003-09-01</BirthDate>
    </Client>
</Clients>
```

[ClientXMLImportData.cs](ClientXMLApp/Services/DTOs/ClientXMLImportData.cs) maps that structure
with serialization attributes:

| XML | C# member | Attribute |
| --- | --- | --- |
| `<Clients>` document root | `XmlClientList` | `[XmlRoot("Clients")]` |
| repeated `<Client>` | `XmlClientList.Clients` | `[XmlElement("Client")]` |
| `ID="12345"` | `XmlClient.ID` (`int`) | `[XmlAttribute("ID")]` |
| `<Name>` | `XmlClient.Name` | by convention |
| `<Addresses>` wrapper | `XmlClient.Addresses` | `[XmlArray("Addresses")]` |
| each `<Address>` | list item | `[XmlArrayItem("Address")]` |
| `Type="1"` | `XmlAddress.Type` (`int`) | `[XmlAttribute("Type")]` |
| `Home address` (element body) | `XmlAddress.AddressText` | `[XmlText]` |
| `<BirthDate>` | `XmlClient.BirthDate` (`DateTime`) | by convention |

`Name` and `BirthDate` need no attribute because the element names already match the property
names, and `BirthDate` parses straight into `DateTime`. The `Type` attribute is read as an
integer and cast to the [AddressType](ClientXMLApp/Models/Address.cs) enum
(`Unknown = 0`, `Home = 1`, `Public = 2`). The `ID` attribute is deserialized but not carried
into the insert; keys are assigned by the database identity column.

### Import flow

1. [Import.cshtml.cs](ClientXMLApp/Pages/Clients/Import.cshtml.cs) accepts the upload as an
   `IFormFile`, rejects an empty selection, and writes the file to a temporary path.
2. `ImportClientsAsync` deserializes it with `new XmlSerializer(typeof(XmlClientList))` over a
   `StreamReader` and projects the result into `AddClientDto` and `AddressDto` instances.
3. `AddClientsAsync` writes every client and its addresses inside one transaction. A failure
   part-way through rolls the whole file back.
4. The temporary file is deleted, and the page reports either success or the exception message.

## Validation

Validation lives on the DTOs as data annotations, so the same rules apply to form posts and to
imported records. `AddClientDto` requires a name of at least three characters and a birth date;
`AddressDto` requires an address text of at least five characters and a type. `ModelState`
gates the create post. The add-client page also runs jQuery unobtrusive validation and keeps
the submit button disabled until the form is valid.

## Data model

`Client` has an identity key, a name, a birth date and a collection of addresses. `Address` has
an identity key, an `AddressType`, its text, and a `ClientID` foreign key.
[AppDbContext](ClientXMLApp/Data/AppDbContext.cs) configures the keys, the generated values and
the one-to-many relationship in `OnModelCreating`.

The [InitialCreate](ClientXMLApp/Migrations/20240725075238_InitialCreate.cs) migration creates
both tables with `SqlServer:Identity` columns, an index on `Addresses.ClientID`, and a
cascade-delete foreign key back to `Clients`.

## JSON export

The client list page serializes the current view models into the page, then builds the file in
the browser from a `Blob` and an object URL, and saves it as `clients.json`. The export
reflects whatever sort order is applied at the time.

## Running it

Requirements: the .NET 8 SDK, a reachable SQL Server instance, and the EF Core CLI
(`dotnet tool install --global dotnet-ef`).

Configuration files are excluded from version control, so add
`ClientXMLApp/appsettings.json` with a connection string named `local`, which is the name
`Program.cs` reads:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "local": "Server=(localdb)\\MSSQLLocalDB;Database=ClientXMLAppDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "AllowedHosts": "*"
}
```

Then create the schema and start the app:

```bash
dotnet restore
dotnet ef database update --project ClientXMLApp
dotnet run --project ClientXMLApp
```

The listening URL is printed on startup. From the home page, `Import XML` takes
`client_import_example.xml`, and `Clients` shows the imported records with their addresses.

Static client libraries live under `wwwroot/`, which is also excluded from version control.
Restore Bootstrap and jQuery there, or repoint the references in
[_Layout.cshtml](ClientXMLApp/Pages/Shared/_Layout.cshtml) and
[_ValidationScriptsPartial.cshtml](ClientXMLApp/Pages/Shared/_ValidationScriptsPartial.cshtml)
at CDN copies. The app runs without them; only styling and in-browser validation are affected,
and server-side validation is unchanged.

## Project layout

```
ClientXMLApp/
  Data/            AppDbContext, IUnitOfWork, UnitOfWork
  Models/          Client, Address, AddressType
  Repositories/    IClientRepository, IAddressRepository and implementations
  Services/        IClientService, IClientImportService, MappingProfile
    DTOs/          boundary types and the XML serialization types
  Migrations/      InitialCreate and the model snapshot
  Pages/
    Clients/       Create, Import, View
client_import_example.xml
```

## Scope

A self-contained practice project, built to work through XML deserialization with attribute
mapping and a clean separation between pages, services, and data access. The feature set covers
import, create, list, sort, and export. Update and delete exist in the service and repository
layers but have no page in front of them, and there is no authentication, paging, or test
project.
