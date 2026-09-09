# ClientXMLApp

An ASP.NET Core Razor Pages application for importing client records from XML and managing
them against SQL Server. Clients come in through a web form or as an uploaded XML file with
nested address lists. Both paths validate the input against the same rules and write it in a
single database round trip.

The XML import is the part worth reading. It takes a `Clients -> Client -> Addresses -> Address`
document where identifiers and address types are attributes and address text is the element
body, and maps it onto plain C# classes with `XmlSerializer` and serialization attributes.

## Features

- Upload an XML file and import every client and address it contains ([Import](ClientXMLApp/Pages/Clients/Import.cshtml))
- Add a single client with a dynamic, add-and-remove address table ([Create](ClientXMLApp/Pages/Clients/Create.cshtml))
- List clients with their addresses, sorted and paged by the database ([View](ClientXMLApp/Pages/Clients/View.cshtml))
- Export every client to a `clients.json` download
- Server-side and client-side validation on client and address input
- Writes that either land completely or leave the tables untouched

## Stack

.NET 8, ASP.NET Core Razor Pages, Entity Framework Core 8.0.30 with the SQL Server provider,
AutoMapper 15.1.3, Bootstrap 5 and jQuery validation for the front end. Tests are xUnit against
the EF Core SQLite provider.

## Architecture

Three layers, wired together by constructor injection and registered in
[Program.cs](ClientXMLApp/Program.cs):

```
Razor Pages  ->  Services  ->  EF Core / SQL Server
```

### Pages

Page models depend on service interfaces only. [Import.cshtml.cs](ClientXMLApp/Pages/Clients/Import.cshtml.cs)
depends on `IClientImportService`; [Create.cshtml.cs](ClientXMLApp/Pages/Clients/Create.cshtml.cs)
and [View.cshtml.cs](ClientXMLApp/Pages/Clients/View.cshtml.cs) depend on `IClientService`.

### Services

[IClientService](ClientXMLApp/Services/IClientService.cs) exposes the client operations:
`GetClientsAsync` for a page of the listing, `GetAllClientsAsync` for the export,
`GetClientByIdAsync`, `AddClientAsync`, `UpdateClientAsync`, `DeleteClientAsync`, and the bulk
`AddClientsAsync` the importer uses. Every method takes a `CancellationToken`, which the page
handlers pass down from the request.

[ClientService](ClientXMLApp/Services/ClientService.cs) is the only class that touches
`AppDbContext`. Sorting and paging are applied to the `IQueryable`, so they become an `ORDER BY`
and an `OFFSET`/`FETCH` the server can satisfy from an index. Addresses travel with their client
into `SaveChanges`, so EF assigns their foreign keys during relationship fixup.

[ClientImportService](ClientXMLApp/Services/ClientImportService.cs) owns XML deserialization
and delegates persistence to `IClientService`, so it holds no query or transaction code
of its own.

### Data access

Services talk to EF Core directly. `DbContext` is the unit of work and `DbSet<T>` is the
repository, so nothing wraps them, and a query stays an `IQueryable` until it is materialised,
which is what allows sorting and paging to run in SQL. Each write is a single
`SaveChangesAsync`, which EF wraps in a transaction of its own.

### DTOs and mapping

Entities never reach a page. [Services/DTOs](ClientXMLApp/Services/DTOs) defines the boundary
types: `AddClientDto`, `UpdateClientDto`, `ViewClientDto`, `AddressDto`, the `ClientQuery` and
`PagedResult<T>` pair that carries a listing request and its answer, the `ClientSortingOptions`
enum, and `ClientXMLImportData.cs`, which holds the three serialization types `XmlClientList`,
`XmlClient` and `XmlAddress`.

[MappingProfile](ClientXMLApp/Services/MappingProfile.cs) registers the entity-to-DTO
conversions with AutoMapper, including the nested address collections, and is picked up by
`AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())`. A test calls
`AssertConfigurationIsValid()`, so an unmapped member fails the build instead of the first
request that hits it.

### Dependency injection

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("local")));

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IClientImportService, ClientImportService>();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
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
   `IFormFile`, rejects an empty selection or anything over 10 MB, and opens its read stream.
   Nothing is written to disk on any path.
2. `ImportClientsAsync` takes that `Stream` and reads it with `XmlSerializer` over an
   `XmlReader` configured with `DtdProcessing.Prohibit` and no resolver, so an uploaded file
   cannot pull in an external entity or expand one of its own.
3. Every record is checked against the DTO annotations before anything is written. One bad
   record rejects the file, and the message names which record and which rule.
4. `AddClientsAsync` inserts the batch with a single `SaveChangesAsync`, and the page reports
   how many clients were imported.

A file the importer will not accept raises `ClientImportException`, whose message is written for
whoever uploaded it. The page shows that and logs the cause. Anything else is a fault: outside
Development the pipeline sends it to `/Error`, so no exception message reaches a browser.

## Validation

Validation lives on the DTOs as data annotations, so the same rules apply to form posts and to
imported records. `AddClientDto` requires a name of 3 to 200 characters and a birth date;
`AddressDto` requires an address text of 5 to 400 characters and a type. `ModelState`
gates the create post, `ClientImportService` runs the same annotations over each imported
record, and the add-client page also runs jQuery unobtrusive validation and keeps the submit
button disabled until the form is valid. The two length bounds match the column widths, so
over-long input is a validation message rather than a truncation error out of SQL Server.

## Listing, sorting and paging

`ClientQuery` carries the sort column, the direction and the window, and clamps all of them
before they reach a query: the page size defaults to 20 and stops at 200, and a sort value that
is not a defined enum member falls back to no sort. `PagedResult<T>` returns the page and the
total row count together, which is what lets the view draw a pager without a second query.
Reads are `AsNoTracking` and split, so including addresses does not multiply out the rows.

## Data model

`Client` has an identity key, a name of up to 200 characters, a birth date and a collection of
addresses. `Address` has an identity key, an `AddressType`, its text of up to 400 characters,
and a `ClientID` foreign key. Keys, identity columns and the foreign key all follow convention,
so [AppDbContext](ClientXMLApp/Data/AppDbContext.cs) configures the cascade and the two sort
indexes and leaves the rest alone. Deleting a client is a single `ExecuteDelete`, and the
declared cascade takes its addresses with it.

The [InitialCreate](ClientXMLApp/Migrations/20240725075238_InitialCreate.cs) migration creates
both tables with `SqlServer:Identity` columns, an index on `Addresses.ClientID`, and a
cascade-delete foreign key back to `Clients`.
[AddStringLengthsAndSortIndexes](ClientXMLApp/Migrations/20260909014012_AddStringLengthsAndSortIndexes.cs)
bounds `Name` and `AddressText` and adds `IX_Clients_Name` and `IX_Clients_BirthDate`, since
`nvarchar(max)` cannot be indexed and the listing sorts on both columns.

## JSON export

A page handler queries the whole table in the current sort order and returns it as a file, so
the export covers every client rather than the page on screen. Address types are written as
names.

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

The tests need none of that, since they bring their own database:

```bash
dotnet test
```

The listening URL is printed on startup. From the home page, `Import XML` takes
`client_import_example.xml`, and `Clients` shows the imported records with their addresses.

Static client libraries live under `wwwroot/`, which is also excluded from version control.
Restore Bootstrap and jQuery there, or repoint the references in
[_Layout.cshtml](ClientXMLApp/Pages/Shared/_Layout.cshtml) and
[_ValidationScriptsPartial.cshtml](ClientXMLApp/Pages/Shared/_ValidationScriptsPartial.cshtml)
at CDN copies. The app runs without them; only styling and in-browser validation are affected,
and server-side validation is unchanged.

## Tests

[tests/ClientXMLApp.Tests](tests/ClientXMLApp.Tests) holds 32 tests that run in about a second.
They use SQLite in memory rather than the EF in-memory provider, since the behaviour under test
belongs to the database: `ORDER BY`, `LIMIT`/`OFFSET`, cascade delete, identity keys.

A `SaveChanges` interceptor counts round trips, which is how a batch insert is held to one save.
A log callback captures the SQL EF sends, so a test can assert the `ORDER BY` and the window are
in the statement.

The importer tests need no database. A recording stand-in for `IClientService` takes the place
of persistence, and they cover the attribute mapping, a truncated document, a wrong root
element, an unparseable date, a declared DTD, an empty document, a name below the minimum, a
name past the column width, and the sample file in the repository root.

Test schemas come from `EnsureCreated()` against the EF model, so the migrations themselves are
not exercised.

## Project layout

```
ClientXMLApp/
  Data/            AppDbContext
  Models/          Client, Address, AddressType
  Services/        IClientService, IClientImportService, MappingProfile, ClientImportException
    DTOs/          boundary types and the XML serialization types
  Migrations/      InitialCreate, AddStringLengthsAndSortIndexes, model snapshot
  Pages/
    Clients/       Create, Import, View
tests/
  ClientXMLApp.Tests/
client_import_example.xml
```

## Scope

A small demonstration build that wires one stack end to end: XML deserialization with attribute
mapping, one set of validation rules across both entry points, a listing the database sorts and
pages, and a schema whose bounds and indexes match the queries it serves. The feature set covers
import, create, list, sort, and export. Update and delete exist in the service with tests and no
page in front of them, and there is no authentication.
