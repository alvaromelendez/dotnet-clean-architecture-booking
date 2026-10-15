using Asp.Versioning;
using Scalar.AspNetCore;

namespace BookingSystem.Api.Infrastructure;

internal static class ApiVersioningSetup
{
    private const string VersionedRoutePrefix = "api/v{version:apiVersion}";

    /// <summary>
    /// URL-segment versioning (<c>/api/v1/...</c>) with one native OpenAPI document per version.
    /// </summary>
    /// <remarks>
    /// Asp.Versioning.OpenApi is the official bridge between Asp.Versioning and Microsoft.AspNetCore.OpenApi:
    /// the API explorer groups endpoints by version ("v1") and substitutes the version into the routes,
    /// so the document shows <c>/api/v1/rooms</c> instead of a <c>{version}</c> parameter.
    /// </remarks>
    public static IServiceCollection AddApiVersioningAndOpenApi(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = ApiVersions.V1;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApi(options => options.Document.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = $"BookingSystem API {options.Description.GroupName}";
                document.Info.Description =
                    "Space reservation management (rooms, courts, offices). Errors follow RFC 9457 " +
                    "problem details and always include `code` and `traceId`.";

                return Task.CompletedTask;
            }));

        return services;
    }

    /// <summary>
    /// Maps every <see cref="Endpoints.IEndpoint"/> under <c>/api/v1</c>. Responses every endpoint can
    /// produce (rate limiting, unexpected errors) are declared once for the whole group.
    /// </summary>
    public static WebApplication MapVersionedEndpoints(this WebApplication app)
    {
        var versionedApi = app.NewVersionedApi();

        versionedApi.MapGroup(VersionedRoutePrefix)
            .HasApiVersion(ApiVersions.V1)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .MapEndpoints(app.Services);

        return app;
    }

    /// <summary>
    /// Exposes <c>/openapi/{version}.json</c> and the Scalar UI at <c>/scalar</c>. Development only.
    /// </summary>
    public static WebApplication MapOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi().WithDocumentPerVersion();

        app.MapScalarApiReference(options =>
        {
            options.WithTitle("BookingSystem API");

            foreach (var description in app.DescribeApiVersions())
            {
                options.AddDocument(description.GroupName, $"BookingSystem API {description.GroupName}");
            }
        });

        return app;
    }
}
