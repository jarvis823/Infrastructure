using MediatR;
using Nacencom.Infrastructure.DataTypes;

namespace Nacencom.Infrastructure.Mediator
{
    public interface IApiRequestHandler<in TRequest> : IRequestHandler<TRequest, ApiResult>
        where TRequest : IRequest<ApiResult>
    {
    }

    public interface IApiRequestHandler<in TRequest, TResponse> : IRequestHandler<TRequest, ApiResult<TResponse>>
        where TRequest : IRequest<ApiResult<TResponse>>
    {
    }

    public interface IPagedApiRequestHandler<in TRequest, TResponse> : IRequestHandler<TRequest, PagedApiResult<TResponse>>
        where TRequest : IRequest<PagedApiResult<TResponse>>
    {
    }
}
