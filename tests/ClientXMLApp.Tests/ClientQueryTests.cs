using ClientXMLApp.Services.DTOs;

namespace ClientXMLApp.Tests
{
    public class ClientQueryTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(int.MinValue, 1)]
        [InlineData(1, 1)]
        [InlineData(7, 7)]
        [InlineData(int.MaxValue, ClientQuery.MaxPageNumber)]
        public void Page_number_is_clamped_to_a_range_that_cannot_overflow(int requested, int expected)
        {
            var normalized = new ClientQuery { PageNumber = requested }.Normalized();

            Assert.Equal(expected, normalized.PageNumber);
        }

        [Fact]
        public void The_largest_allowed_page_still_computes_a_positive_offset()
        {
            var normalized = new ClientQuery
            {
                PageNumber = int.MaxValue,
                PageSize = ClientQuery.MaxPageSize
            }.Normalized();

            var offset = (normalized.PageNumber - 1) * normalized.PageSize;
            var lastItem = normalized.PageNumber * normalized.PageSize;

            Assert.True(offset > 0, $"offset overflowed to {offset}");
            Assert.True(lastItem > 0, $"last item overflowed to {lastItem}");
        }

        [Theory]
        [InlineData(0, ClientQuery.DefaultPageSize)]
        [InlineData(-5, ClientQuery.DefaultPageSize)]
        [InlineData(10, 10)]
        [InlineData(5000, ClientQuery.MaxPageSize)]
        [InlineData(int.MaxValue, ClientQuery.MaxPageSize)]
        public void Page_size_falls_back_and_caps(int requested, int expected)
        {
            var normalized = new ClientQuery { PageSize = requested }.Normalized();

            Assert.Equal(expected, normalized.PageSize);
        }

        [Fact]
        public void A_sort_value_outside_the_enum_falls_back_to_no_sort()
        {
            var normalized = new ClientQuery { SortBy = (ClientSortingOptions)99 }.Normalized();

            Assert.Equal(ClientSortingOptions.None, normalized.SortBy);
        }

        [Fact]
        public void Normalising_does_not_mutate_the_original()
        {
            var query = new ClientQuery { PageNumber = 0, PageSize = 5000 };

            query.Normalized();

            Assert.Equal(0, query.PageNumber);
            Assert.Equal(5000, query.PageSize);
        }
    }
}
