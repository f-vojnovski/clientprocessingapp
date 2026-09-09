using ClientXMLApp.Data;
using ClientXMLApp.Models;
using ClientXMLApp.Services.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ClientXMLApp.Tests
{
    public class ClientServiceTests : IDisposable
    {
        private readonly SqliteDatabase _db = new SqliteDatabase();

        public void Dispose() => _db.Dispose();

        private void Seed(params (string Name, int BirthYear)[] clients)
        {
            using var context = _db.NewContext();
            foreach (var (name, birthYear) in clients)
            {
                context.Clients.Add(new Client
                {
                    Name = name,
                    BirthDate = new DateTime(birthYear, 1, 1),
                    Addresses = new List<Address>
                    {
                        new Address { AddressText = name + " home address", Type = AddressType.Home }
                    }
                });
            }

            context.SaveChanges();
        }

        [Fact]
        public async Task Sorts_in_the_database_rather_than_after_paging()
        {
            // Sorting the page in memory returns Alice and Bob; sorting in SQL returns Zach
            // and Yolanda, so the two orderings disagree on the answer.
            Seed(("Alice", 1990), ("Bob", 1991), ("Yolanda", 1992), ("Zach", 1993));

            var page = await _db.NewService().GetClientsAsync(new ClientQuery
            {
                SortBy = ClientSortingOptions.Name,
                SortAscending = false,
                PageNumber = 1,
                PageSize = 2
            });

            Assert.Equal(new[] { "Zach", "Yolanda" }, page.Items.Select(c => c.Name));
            Assert.Equal(4, page.TotalCount);
        }

        [Fact]
        public async Task Puts_the_sort_and_the_page_into_the_sql_it_sends()
        {
            Seed(("Alice", 1990), ("Bob", 1991), ("Cleo", 1992));
            _db.Sql.Clear();

            await _db.NewService().GetClientsAsync(new ClientQuery
            {
                SortBy = ClientSortingOptions.Name,
                PageNumber = 2,
                PageSize = 1
            });

            // Split query: the clients, then their addresses. Both carry the order and window.
            var paged = _db.Sql
                .Where(sql => sql.Contains("FROM \"Clients\"", StringComparison.Ordinal))
                .Where(sql => sql.Contains("LIMIT", StringComparison.Ordinal))
                .ToList();

            Assert.NotEmpty(paged);
            Assert.All(paged, sql =>
            {
                Assert.Contains("ORDER BY", sql);
                Assert.Contains("OFFSET", sql);
            });
        }

        [Fact]
        public async Task Sorts_by_birth_date_in_both_directions()
        {
            Seed(("Alice", 1993), ("Bob", 1990), ("Cleo", 1991));

            var service = _db.NewService();

            var ascending = await service.GetClientsAsync(new ClientQuery
            {
                SortBy = ClientSortingOptions.BirthDate,
                SortAscending = true
            });
            var descending = await service.GetClientsAsync(new ClientQuery
            {
                SortBy = ClientSortingOptions.BirthDate,
                SortAscending = false
            });

            Assert.Equal(new[] { "Bob", "Cleo", "Alice" }, ascending.Items.Select(c => c.Name));
            Assert.Equal(new[] { "Alice", "Cleo", "Bob" }, descending.Items.Select(c => c.Name));
        }

        [Fact]
        public async Task Returns_only_the_requested_page()
        {
            Seed(Enumerable.Range(1, 25).Select(i => ($"Client {i:00}", 1990)).ToArray());

            var page = await _db.NewService().GetClientsAsync(new ClientQuery
            {
                PageNumber = 2,
                PageSize = 10
            });

            Assert.Equal(10, page.Items.Count);
            Assert.Equal(25, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
            Assert.True(page.HasPreviousPage);
            Assert.True(page.HasNextPage);
            Assert.Equal("Client 11", page.Items[0].Name);
            Assert.Equal(11, page.FirstItemOnPage);
            Assert.Equal(20, page.LastItemOnPage);
        }

        [Fact]
        public async Task Walking_the_pages_returns_every_row_exactly_once()
        {
            Seed(Enumerable.Range(1, 7).Select(i => ($"Client {i}", 1990)).ToArray());
            var service = _db.NewService();

            var seen = new List<int>();
            for (var pageNumber = 1; pageNumber <= 4; pageNumber++)
            {
                var page = await service.GetClientsAsync(new ClientQuery
                {
                    SortBy = ClientSortingOptions.Name,
                    PageNumber = pageNumber,
                    PageSize = 2
                });
                seen.AddRange(page.Items.Select(c => c.ID));
            }

            Assert.Equal(7, seen.Count);
            Assert.Equal(7, seen.Distinct().Count());
        }

        [Fact]
        public async Task Clamps_a_page_request_that_arrives_out_of_range()
        {
            Seed(("Alice", 1990));

            var page = await _db.NewService().GetClientsAsync(new ClientQuery
            {
                PageNumber = 0,
                PageSize = 5000
            });

            Assert.Equal(1, page.PageNumber);
            Assert.Equal(ClientQuery.MaxPageSize, page.PageSize);
        }

        [Fact]
        public async Task Loads_the_addresses_of_every_client_on_the_page()
        {
            Seed(("Alice", 1990), ("Bob", 1991));

            var page = await _db.NewService().GetClientsAsync(new ClientQuery());

            Assert.All(page.Items, client => Assert.Single(client.Addresses));
            Assert.Equal("Alice home address", page.Items[0].Addresses.First().AddressText);
        }

        [Fact]
        public async Task Writes_a_whole_batch_in_one_round_trip()
        {
            var batch = Enumerable.Range(1, 5).Select(i => new AddClientDto
            {
                Name = $"Client {i}",
                BirthDate = new DateTime(1990, 1, 1),
                Addresses = new List<AddressDto>
                {
                    new AddressDto { AddressText = "Home address", Type = AddressType.Home },
                    new AddressDto { AddressText = "Weekend address", Type = AddressType.Public }
                }
            }).ToList();

            var before = _db.SaveChanges.Count;
            await _db.NewService().AddClientsAsync(batch);

            Assert.Equal(1, _db.SaveChanges.Count - before);

            using var context = _db.NewContext();
            Assert.Equal(5, await context.Clients.CountAsync());
            Assert.Equal(10, await context.Addresses.CountAsync());
        }

        [Fact]
        public async Task Assigns_address_foreign_keys_without_setting_them_by_hand()
        {
            var id = await _db.NewService().AddClientAsync(new AddClientDto
            {
                Name = "Alice",
                BirthDate = new DateTime(1990, 1, 1),
                Addresses = new List<AddressDto>
                {
                    new AddressDto { AddressText = "Home address", Type = AddressType.Home }
                }
            });

            using var context = _db.NewContext();
            var addresses = await context.Addresses.Where(a => a.ClientID == id).ToListAsync();

            Assert.True(id > 0);
            Assert.Single(addresses);
        }

        [Fact]
        public async Task Writes_one_client_in_one_round_trip()
        {
            var before = _db.SaveChanges.Count;

            await _db.NewService().AddClientAsync(new AddClientDto
            {
                Name = "Alice",
                BirthDate = new DateTime(1990, 1, 1),
                Addresses = new List<AddressDto>
                {
                    new AddressDto { AddressText = "Home address", Type = AddressType.Home }
                }
            });

            Assert.Equal(1, _db.SaveChanges.Count - before);
        }

        [Fact]
        public async Task Deletes_addresses_through_the_declared_cascade()
        {
            Seed(("Alice", 1990));
            int id;
            using (var context = _db.NewContext())
            {
                id = await context.Clients.Select(c => c.ID).FirstAsync();
            }

            var deleted = await _db.NewService().DeleteClientAsync(id);

            using var after = _db.NewContext();
            Assert.True(deleted);
            Assert.Equal(0, await after.Clients.CountAsync());
            Assert.Equal(0, await after.Addresses.CountAsync());
        }

        [Fact]
        public async Task Reports_a_missing_client_on_delete_without_throwing()
        {
            var deleted = await _db.NewService().DeleteClientAsync(4242);

            Assert.False(deleted);
        }

        [Fact]
        public async Task Reports_a_missing_client_on_update_without_throwing()
        {
            var updated = await _db.NewService().UpdateClientAsync(new UpdateClientDto
            {
                ID = 4242,
                Name = "Nobody",
                BirthDate = new DateTime(1990, 1, 1),
                Addresses = new List<AddressDto>()
            });

            Assert.False(updated);
        }

        [Fact]
        public async Task Replaces_the_address_set_on_update()
        {
            Seed(("Alice", 1990));
            int id;
            using (var context = _db.NewContext())
            {
                id = await context.Clients.Select(c => c.ID).FirstAsync();
            }

            var updated = await _db.NewService().UpdateClientAsync(new UpdateClientDto
            {
                ID = id,
                Name = "Alice Renamed",
                BirthDate = new DateTime(1991, 2, 3),
                Addresses = new List<AddressDto>
                {
                    new AddressDto { AddressText = "Replacement address", Type = AddressType.Public }
                }
            });

            using var after = _db.NewContext();
            var client = await after.Clients.Include(c => c.Addresses).SingleAsync(c => c.ID == id);

            Assert.True(updated);
            Assert.Equal("Alice Renamed", client.Name);
            Assert.Equal(new DateTime(1991, 2, 3), client.BirthDate);
            Assert.Single(client.Addresses);
            Assert.Equal("Replacement address", client.Addresses.First().AddressText);
            Assert.Equal(1, await after.Addresses.CountAsync());
        }

        [Fact]
        public async Task Returns_every_client_for_the_export_in_sort_order()
        {
            Seed(("Zach", 1990), ("Alice", 1991));

            var all = await _db.NewService().GetAllClientsAsync(ClientSortingOptions.Name, sortAscending: true);

            Assert.Equal(new[] { "Alice", "Zach" }, all.Select(c => c.Name));
        }

        [Fact]
        public async Task Returns_null_for_a_client_that_does_not_exist()
        {
            Assert.Null(await _db.NewService().GetClientByIdAsync(4242));
        }
    }
}
