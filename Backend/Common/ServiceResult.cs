using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SEAL.NET.Common
{
    /// <summary>
    /// Transport-agnostic outcome of a service operation. Lets the Application/Service
    /// layer express *what* happened (success, not found, forbidden, invalid request)
    /// without taking a dependency on ASP.NET Core / HTTP. The controller maps it to an
    /// <see cref="IActionResult"/> once, via <see cref="ServiceResultExtensions.ToActionResult"/>.
    /// </summary>
    public enum ResultStatus
    {
        Ok,
        Created,
        BadRequest,
        NotFound,
        Forbidden,
        Unauthorized,
        Conflict
    }

    public sealed class ServiceResult
    {
        public ResultStatus Status { get; }

        /// <summary>Optional JSON body to return to the client (message, payload, etc.).</summary>
        public object? Body { get; }

        public bool Succeeded => Status == ResultStatus.Ok;

        private ServiceResult(ResultStatus status, object? body)
        {
            Status = status;
            Body = body;
        }

        public static ServiceResult Ok(object? body = null) => new(ResultStatus.Ok, body);
        public static ServiceResult Created(object body) => new(ResultStatus.Created, body);
        public static ServiceResult BadRequest(object body) => new(ResultStatus.BadRequest, body);
        public static ServiceResult NotFound(object body) => new(ResultStatus.NotFound, body);
        public static ServiceResult Forbidden(object? body = null) => new(ResultStatus.Forbidden, body);
        public static ServiceResult Unauthorized(object body) => new(ResultStatus.Unauthorized, body);
        public static ServiceResult Conflict(object body) => new(ResultStatus.Conflict, body);
    }

    public static class ServiceResultExtensions
    {
        /// <summary>
        /// Maps a <see cref="ServiceResult"/> to the matching HTTP response. This is the only
        /// place that knows the status-to-HTTP mapping, so controllers stay free of it.
        /// </summary>
        public static IActionResult ToActionResult(this ServiceResult result, ControllerBase controller) => result.Status switch
        {
            ResultStatus.Ok => controller.Ok(result.Body),
            ResultStatus.Created => controller.StatusCode(StatusCodes.Status201Created, result.Body),
            ResultStatus.BadRequest => controller.BadRequest(result.Body),
            ResultStatus.NotFound => controller.NotFound(result.Body),
            // Forbid() (no body) preserves the auth-challenge semantics most actions used;
            // when a body is supplied we return a plain 403 with that JSON instead.
            ResultStatus.Forbidden => result.Body == null
                ? controller.Forbid()
                : controller.StatusCode(StatusCodes.Status403Forbidden, result.Body),
            ResultStatus.Unauthorized => controller.Unauthorized(result.Body),
            ResultStatus.Conflict => controller.Conflict(result.Body),
            _ => controller.StatusCode(500, result.Body)
        };
    }
}
