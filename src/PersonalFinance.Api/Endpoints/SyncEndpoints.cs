using MediatR;
using PersonalFinance.Application.Sync;

namespace PersonalFinance.Api.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync").WithTags("Sync").RequireAuthorization();

        group.MapPost("/push", async (PushSyncCommand command, ISender sender) =>
        {
            await sender.Send(command);
            return Results.NoContent();
        });

        group.MapGet("/pull", async (DateTime? since, ISender sender) =>
            Results.Ok(await sender.Send(new PullSyncQuery(since))));

        return app;
    }
}
