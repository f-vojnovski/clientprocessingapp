namespace ClientXMLApp.Services.DTOs
{
    public class PagedResult<T>
    {
        public PagedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        public IReadOnlyList<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }

        public int TotalPages => PageSize < 1 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public int FirstItemOnPage => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
        public int LastItemOnPage => Math.Min(PageNumber * PageSize, TotalCount);
    }
}
