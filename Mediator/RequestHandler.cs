using MediatR;
using Microsoft.Extensions.Logging;
using Nacencom.Infrastructure.DataTypes;
using System.Net;

namespace Nacencom.Infrastructure.Mediator
{
    public abstract class ApiRequestHandler<TRequest> : ExceptionHandler<TRequest, ApiResult>,
        IRequestHandler<TRequest, ApiResult>
        where TRequest : IRequest<ApiResult>
    {
        public ApiRequestHandler(ILogger<ApiRequestHandler<TRequest>> logger) : base(logger)
        {
        }

        public virtual async Task<ApiResult> Handle(TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await HandleAsync(request, cancellationToken);
                return new ApiResult((int)HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, request, cancellationToken);
            }
        }

        public abstract Task HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    public abstract class ApiRequestHandler<TRequest, TResponse> : ExceptionHandler<TRequest, ApiResult<TResponse>>,
        IRequestHandler<TRequest, ApiResult<TResponse>>
        where TRequest : IRequest<ApiResult<TResponse>>
    {
        public ApiRequestHandler(ILogger<ApiRequestHandler<TRequest, TResponse>> logger) : base(logger)
        {
        }

        public virtual async Task<ApiResult<TResponse>> Handle(TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await HandleAsync(request, cancellationToken);
                return new ApiResult<TResponse>
                {
                    Data = result,
                    Status = (int)HttpStatusCode.OK,
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, request, cancellationToken);
            }
        }

        public abstract Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    public abstract class PagedApiRequestHandler<TRequest, TResponse> : ExceptionHandler<TRequest, PagedApiResult<TResponse>>,
        IRequestHandler<TRequest, PagedApiResult<TResponse>>
        where TRequest : IPagedApiRequest<TResponse>
    {
        private static readonly List<TResponse> _empty = new();

        public PagedApiRequestHandler(ILogger<PagedApiRequestHandler<TRequest, TResponse>> logger) : base(logger)
        {
        }

        public virtual async Task<PagedApiResult<TResponse>> Handle(TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await HandleAsync(request, cancellationToken);
                return new PagedApiResult<TResponse>
                {
                    Page = result.Page,
                    PageSize = result.PageSize,
                    TotalRecords = result.TotalRecords,
                    Data = result.Items?.ToList() ?? _empty,
                    Status = (int)HttpStatusCode.OK,
                    Success = true,
                    MetaData = result.MetaData
                };
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, request, cancellationToken);
            }
        }

        public abstract Task<PagedList<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }

    public abstract class ExceptionHandler<TRequest, TResponse>
        where TResponse : ApiResult, new()
        where TRequest : IRequest<ApiResult>
    {
        private readonly ILogger<ExceptionHandler<TRequest, TResponse>> _logger;

        public ExceptionHandler(ILogger<ExceptionHandler<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public virtual Task<TResponse> HandleExceptionAsync(Exception ex, TRequest request, CancellationToken cancellationToken)
        {
            throw ex;
        }
    }
}
