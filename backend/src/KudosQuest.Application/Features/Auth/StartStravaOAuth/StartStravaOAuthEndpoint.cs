using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Strava;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KudosQuest.Application.Features.Auth.StartStravaOAuth;

public static class StartStravaOAuthEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/api/auth/strava",
                (IOAuthStateStore stateStore, IStravaOAuthClient strava) =>
                {
                    var state = stateStore.Create();
                    var url = strava.BuildAuthorizeUrl(state);
                    return Results.Redirect(url);
                }
            )
            .WithName("StartStravaOAuth")
            .WithTags("Auth");
}
