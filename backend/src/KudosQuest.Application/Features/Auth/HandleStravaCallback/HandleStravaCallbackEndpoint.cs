using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KudosQuest.Application.Features.Auth.HandleStravaCallback;

public static class HandleStravaCallbackEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/api/auth/callback",
                async (
                    string? code,
                    string? scope,
                    string? state,
                    string? error,
                    HandleStravaCallbackHandler handler,
                    CancellationToken cancellationToken
                ) =>
                {
                    try
                    {
                        var result = await handler.HandleAsync(
                            new HandleStravaCallbackCommand(code, scope, state, error),
                            cancellationToken
                        );

                        return Results.Ok(result);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Bad Request",
                            detail: ex.Message
                        );
                    }
                }
            )
            .WithName("HandleStravaCallback")
            .WithTags("Auth");
}
