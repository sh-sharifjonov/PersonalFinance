using MediatR;
using PersonalFinance.Application.Transactions;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions").WithTags("Transactions").RequireAuthorization();

        group.MapGet("/", async (Guid? accountId, ISender sender) =>
            Results.Ok(await sender.Send(new GetTransactionsQuery(accountId))));

        group.MapPost("/", async (CreateTransactionCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPut("/{id:guid}", async (Guid id, UpdateTransactionBody body, ISender sender) =>
        {
            await sender.Send(new UpdateTransactionCommand(id, body.AccountId, body.CategoryId, body.Type, body.Amount, body.Date, body.Note));
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            await sender.Send(new DeleteTransactionCommand(id));
            return Results.NoContent();
        });

        return app;
    }

    public record UpdateTransactionBody(Guid AccountId, Guid CategoryId, TransactionType Type, decimal Amount, DateOnly Date, string? Note);
}
