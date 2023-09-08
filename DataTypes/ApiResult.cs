namespace Nacencom.Infrastructure.DataTypes
{
    public class ApiResult
    {
        public ApiResult()
        {

        }

        public ApiResult(int status, bool success, params string[] errors)
        {
            Status = status;
            Success = success;
            Errors = errors?.Length > 0 ? errors : null;
        }

        public bool Success { get; set; }
        public int Status { get; set; }
        public string[] Errors { get; set; }
        public object MetaData { get; set; }
    }

    public class ApiResult<T> : ApiResult
    {
        public T Data { get; set; } = default!;
    }

    public class PagedApiResult<T> : ApiResult
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalRecords { get; set; }
        public long TotalPage => PageSize == 0 ? 0 : ((TotalRecords - 1) / PageSize + 1);
        public bool HasNextPage => Page < TotalPage;
        public bool HasPreviousPage => Page > 1;
        public List<T> Data { get; set; }
    }
}
