using Microsoft.Data.SqlClient;
using MyApp.App.ErrorHandling;
using System.Net;
using System.Text.Json;

namespace MyApp.App.Middleware
{
    //https://www.syncfusion.com/blogs/post/global-exception-handling-in-net-6.aspx
    public class GlobalErrorHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<GlobalErrorHandlingMiddleware> logger;

        public GlobalErrorHandlingMiddleware(ILogger<GlobalErrorHandlingMiddleware> logger)
        {
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            try
            {
                await next(httpContext);
            }
            catch (Exception ex)
            {
                //logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            HttpResponse response = context.Response;
            ErrorResponse errorResponse = new ErrorResponse();
            var exType = ex.GetType();

            if (typeof(AppException) == exType)
            {
                if (ex.Message.Contains("Invalid Token"))
                {
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.Message = ex.Message;
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                    errorResponse.Message = ex.Message;
                }
            }
            else if (typeof(SqlException) == exType)
            {
                // if (ex.Number == 11001) { } // No such host is known
                response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                errorResponse.Message = ex.Message;
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "Internal Server Error!";
            }
            logger.LogError(ex.Message);
            string result = JsonSerializer.Serialize(errorResponse);
            response.Redirect("/error", false);
            await context.Response.WriteAsync(result);
        }
    }
}
