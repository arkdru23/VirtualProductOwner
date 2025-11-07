using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace BlazorApp1.Extensions;

/// <summary>
/// No-op extension to make .DisableAntiforgery() calls compile in environments
/// where the framework extension is not present. We perform manual CSRF validation
/// in the handlers, so this does not change runtime behavior.
/// </summary>
public static class AntiforgeryNoopExtensions
{
    public static TBuilder DisableAntiforgery<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder;
    }
}
