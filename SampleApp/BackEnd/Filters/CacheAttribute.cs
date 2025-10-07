using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BackEnd.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CacheAttribute : ActionFilterAttribute
{
    private readonly int _timeToLiveSeconds;

    public CacheAttribute(int timeToLiveSeconds = 1000)
    {
        _timeToLiveSeconds = timeToLiveSeconds;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Response.GetTypedHeaders().CacheControl =
            new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(_timeToLiveSeconds)
            };
        base.OnActionExecuting(context);
    }
}