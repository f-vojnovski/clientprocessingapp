namespace ClientXMLApp.Services.DTOs
{
    public class ClientQuery
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 200;

        public ClientSortingOptions SortBy { get; set; } = ClientSortingOptions.None;
        public bool SortAscending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;

        public ClientQuery Normalized() => new ClientQuery
        {
            SortBy = Enum.IsDefined(SortBy) ? SortBy : ClientSortingOptions.None,
            SortAscending = SortAscending,
            PageNumber = PageNumber < 1 ? 1 : PageNumber,
            PageSize = PageSize < 1 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize)
        };
    }
}
