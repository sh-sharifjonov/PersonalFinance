using MediatR;
using PersonalFinance.Application.Categories;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories").RequireAuthorization();

        group.MapGet("/", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetCategoriesQuery())));

        group.MapPost("/", async (CreateCategoryCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryBody body, ISender sender) =>
        {
            await sender.Send(new UpdateCategoryCommand(id, body.Name, body.Type, body.Icon, body.Color));
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            await sender.Send(new DeleteCategoryCommand(id));
            return Results.NoContent();
        });

        return app;
    }

    public record UpdateCategoryBody(string Name, CategoryType Type, string? Icon, string? Color);
}
