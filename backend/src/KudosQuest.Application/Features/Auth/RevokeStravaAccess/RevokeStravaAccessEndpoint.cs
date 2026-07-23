using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KudosQuest.Application.Features.Auth.RevokeStravaAccess;

public static class RevokeStravaAccessEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/api/auth/logout",
                async (
                    ClaimsPrincipal user,
                    RevokeStravaAccessHandler handler,
                    CancellationToken cancellationToken
                ) =>
                {
                    try
                    {
                        await handler.HandleAsync(user, cancellationToken);
                        return Results.NoContent();
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "Unauthorized",
                            detail: ex.Message
                        );
                    }
                }
            )
            .RequireAuthorization()
            .WithName("RevokeStravaAccess")
            .WithTags("Auth");
}
