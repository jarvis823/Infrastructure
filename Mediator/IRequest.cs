using MediatR;
using Nacencom.Infrastructure.DataTypes;

namespace Nacencom.Infrastructure.Mediator
{
    public interface IApiRequest : IRequest<ApiResult>
    {

    }

    public interface IApiRequest<TResponse> : IRequest<ApiResult<TResponse>>
    {
    }

    public interface IPagedApiRequest<TResponse> : IRequest<PagedApiResult<TResponse>>
    {
        int Page { get; set; }
        int PageSize { get; set; }
    }
}
