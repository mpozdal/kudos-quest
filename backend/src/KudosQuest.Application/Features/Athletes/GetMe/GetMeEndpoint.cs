using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KudosQuest.Application.Features.Athletes.GetMe;

public static class GetMeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/api/me",
                async (ClaimsPrincipal user, GetMeHandler handler, CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(user, cancellationToken);

                    if (result is null)
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Athlete not found"
                        );
                    }

                    return Results.Ok(result);
                }
            )
            .RequireAuthorization()
            .WithName("GetMe")
            .WithTags("Athletes");
}
