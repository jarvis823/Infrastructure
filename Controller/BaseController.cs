using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Nacencom.Infrastructure.DataTypes;

namespace Nacencom.Infrastructure.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseController : ControllerBase
    {
        private IMediator _mediator;
        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

        [NonAction]
        protected new virtual ObjectResult Ok()
        {
            return base.Ok(new ApiResult(200, true));
        }

        [NonAction]
        protected virtual ObjectResult Ok<T>(T value) where T : ApiResult
        {
            if (value.Status == 204) return StatusCode(204, null);
            return StatusCode(value?.Status ?? 200, value ?? new ApiResult(200, true));
        }

        [NonAction]
        protected new virtual ObjectResult Ok([ActionResultObjectValue] object value)
        {
            return base.Ok(new ApiResult<object>
            {
                Status = 200,
                Success = true,
                Data = value
            });
        }

        [NonAction]
        protected virtual FileContentResult File(FileDownloadResult file)
        {
            return File(file.FileContents, file.ContentType, file.FileName);
        }

        [NonAction]
        protected virtual FileContentResult File(ApiResult<FileDownloadResult> file)
        {
            return File(file.Data.FileContents, file.Data.ContentType, file.Data.FileName);
        }
    }
}
