namespace Nacencom.Infrastructure.DataTypes
{
    public interface IPagedList
    {
        int Page { get; set; }
        int PageSize { get; set; }
        long TotalRecords { get; set; }
        long TotalPage { get; }
        bool HasNextPage { get; }
        bool HasPreviousPage { get; }
    }

    public class PagedList<T> : IPagedList
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalRecords { get; set; }
        public long TotalPage => PageSize == 0 ? 0 : ((TotalRecords - 1) / PageSize + 1);
        public bool HasNextPage => Page < TotalPage;
        public bool HasPreviousPage => Page > 1;
        public IReadOnlyCollection<T> Items { get; set; } = default!;
        public object MetaData { get; set; }
    }
}
