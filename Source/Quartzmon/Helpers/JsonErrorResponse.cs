
using System.Text.Json;

namespace Quartzmon.Helpers
{
#if (NETSTANDARD || NETCOREAPP)
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.AspNetCore.Mvc;

    public class JsonErrorResponseAttribute : ActionFilterAttribute
    {
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = new JsonPascalCaseNamingPolicy()
        };

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                context.Result = new JsonResult(new { ExceptionMessage = context.Exception.Message }, _serializerOptions) { StatusCode = 400 };
                context.ExceptionHandled = true;
            }
        }
    }
#endif

}
