using MediatR;
using PersonalFinance.Application.Auth;

namespace PersonalFinance.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/login", async (LoginCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        return app;
    }
}
