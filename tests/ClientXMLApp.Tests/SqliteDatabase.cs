using AutoMapper;
using ClientXMLApp.Data;
using ClientXMLApp.Services;
using ClientXMLApp.Tests.Fakes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClientXMLApp.Tests
{
    // SQLite rather than the in-memory provider: these tests are about ORDER BY, LIMIT/OFFSET,
    // cascade delete and identity keys, none of which the in-memory provider implements.
    public sealed class SqliteDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public SqliteDatabase()
        {
            // The database lives as long as the connection, so the fixture holds it open.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            using (var command = _connection.CreateCommand())
            {
                // SQLite only honours declared cascades when this is on.
                command.CommandText = "PRAGMA foreign_keys = ON;";
                command.ExecuteNonQuery();
            }

            SaveChanges = new SaveChangesCounter();
            Readers = new DataReaderTracker();
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(SaveChanges, Readers)
                .LogTo(Sql.Add, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
                .Options;

            using var context = new AppDbContext(_options);
            context.Database.EnsureCreated();

            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        }

        public SaveChangesCounter SaveChanges { get; }

        public DataReaderTracker Readers { get; }

        public IMapper Mapper { get; }

        // Every command EF sent, so a test can assert on the SQL it produced.
        public List<string> Sql { get; } = new List<string>();

        public AppDbContext NewContext() => new AppDbContext(_options);

        public ClientService NewService() => new ClientService(NewContext(), Mapper);

        public void Dispose() => _connection.Dispose();
    }
}
