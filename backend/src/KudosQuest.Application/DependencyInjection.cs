using KudosQuest.Application.Common.Auth;
using KudosQuest.Application.Common.Persistence;
using KudosQuest.Application.Common.Strava;
using KudosQuest.Application.Features.Athletes.GetMe;
using KudosQuest.Application.Features.Auth.HandleStravaCallback;
using KudosQuest.Application.Features.Auth.RevokeStravaAccess;
using KudosQuest.Application.Features.Auth.StartStravaOAuth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KudosQuest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<StravaOptions>()
            .Bind(configuration.GetSection(StravaOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.ClientId)
                    && !string.IsNullOrWhiteSpace(options.ClientSecret)
                    && !string.IsNullOrWhiteSpace(options.RedirectUri),
                "Strava ClientId, ClientSecret and RedirectUri are required."
            )
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Key) && options.Key.Length >= 32,
                "Jwt:Key must be at least 32 characters."
            )
            .ValidateOnStart();

        var connectionString =
            configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

        services.AddDataProtection();
        services.AddSingleton<ISensitiveDataProtector, SensitiveDataProtector>();

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddHttpClient<IStravaOAuthClient, StravaOAuthClient>();
        services.AddSingleton<IOAuthStateStore, InMemoryOAuthStateStore>();
        services.AddScoped<IStravaTokenStore, EfStravaTokenStore>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<HandleStravaCallbackHandler>();
        services.AddScoped<GetMeHandler>();
        services.AddScoped<RevokeStravaAccessHandler>();
        services.AddScoped<IStravaAccessTokenProvider, StravaAccessTokenProvider>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(
                (bearerOptions, jwtOptions) =>
                {
                    bearerOptions.MapInboundClaims = false;
                    bearerOptions.TokenValidationParameters =
                        JwtTokenService.CreateValidationParameters(jwtOptions.Value);
                }
            );

        services.AddAuthorization();
        services.AddProblemDetails();

        return services;
    }

    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        StartStravaOAuthEndpoint.Map(app);
        HandleStravaCallbackEndpoint.Map(app);
        RevokeStravaAccessEndpoint.Map(app);
        GetMeEndpoint.Map(app);
        return app;
    }
}
