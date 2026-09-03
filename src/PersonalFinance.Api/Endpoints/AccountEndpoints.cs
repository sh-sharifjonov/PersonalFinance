using MediatR;
using PersonalFinance.Application.Accounts;

namespace PersonalFinance.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts").RequireAuthorization();

        group.MapGet("/", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetAccountsQuery())));

        group.MapPost("/", async (CreateAccountCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPut("/{id:guid}", async (Guid id, UpdateAccountBody body, ISender sender) =>
        {
            await sender.Send(new UpdateAccountCommand(id, body.Name, body.Currency));
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            await sender.Send(new DeleteAccountCommand(id));
            return Results.NoContent();
        });

        return app;
    }

    public record UpdateAccountBody(string Name, string Currency);
}
