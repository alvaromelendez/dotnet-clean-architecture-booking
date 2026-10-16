using System.Net;
using System.Text.Json;
using BookingSystem.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingSystem.IntegrationTests.Api;

/// <summary>
/// Contract smoke test over the OpenAPI document served in Development: every operation must
/// declare the client errors it can actually return, so generated clients and readers of the
/// docs are not surprised. Adding an endpoint fails <see cref="Document_ListsExactlyTheExpectedOperations"/>
/// until its expected responses are added below.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class OpenApiContractTests(PostgresFixture fixture)
{
    private const string DocumentPath = "/openapi/v1.json";

    private static readonly int[] ClientErrorStatuses = [400, 404, 409];

    /// <summary>
    /// 400: the operation takes input that can be malformed or invalid (body, route id, query).
    /// 404: it addresses a resource by id (or references one, like the room of a new booking).
    /// 409: it changes state that can conflict (duplicate name, overlapping slot, invalid transition).
    /// </summary>
    private static readonly Dictionary<string, int[]> ExpectedClientErrors = new()
    {
        ["POST /api/v1/rooms"] = [400, 409],
        ["GET /api/v1/rooms"] = [400],
        ["GET /api/v1/rooms/available"] = [400],
        ["GET /api/v1/rooms/{id}"] = [400, 404],
        ["PUT /api/v1/rooms/{id}"] = [400, 404, 409],
        ["POST /api/v1/rooms/{id}/activate"] = [400, 404, 409],
        ["POST /api/v1/rooms/{id}/deactivate"] = [400, 404, 409],
        ["POST /api/v1/bookings"] = [400, 404, 409],
        ["GET /api/v1/bookings/{id}"] = [400, 404],
        ["POST /api/v1/bookings/{id}/confirm"] = [400, 404, 409],
        ["POST /api/v1/bookings/{id}/cancel"] = [400, 404, 409],
        ["POST /api/v1/bookings/{id}/complete"] = [400, 404, 409],
        ["GET /api/v1/users/{userId}/bookings"] = [400],
    };

    public static TheoryData<string> Operations => [.. ExpectedClientErrors.Keys];

    [Fact]
    public async Task Document_ListsExactlyTheExpectedOperations()
    {
        var operations = await GetOperationsAsync();

        operations.Keys.ShouldBe(ExpectedClientErrors.Keys, ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Operation_DeclaresTheClientErrorsItCanReturn(string operation)
    {
        var responses = (await GetOperationsAsync())[operation];

        var declaredClientErrors = ClientErrorStatuses.Where(status => responses.ContainsKey(status)).ToArray();

        declaredClientErrors.ShouldBe(ExpectedClientErrors[operation], $"{operation} declares {string.Join(", ", declaredClientErrors)}");
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Operation_DeclaresErrorsAsProblemDetails(string operation)
    {
        var responses = (await GetOperationsAsync())[operation];

        responses.ShouldContainKey(429);
        responses.ShouldContainKey(500);
        foreach (var (status, mediaTypes) in responses.Where(response => response.Key >= 400))
        {
            mediaTypes.ShouldBe([ApiTestClient.ProblemJson], $"{operation} → {status}");
        }
    }

    /// <summary>
    /// Operations keyed as "METHOD /path", each with its declared status codes and their media types.
    /// </summary>
    private async Task<Dictionary<string, Dictionary<int, string[]>>> GetOperationsAsync()
    {
        using var client = fixture.Api.CreateClient();
        var response = await client.GetAsync(DocumentPath);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(operation => (
                Key: $"{operation.Name.ToUpperInvariant()} {path.Name}",
                Responses: operation.Value.GetProperty("responses").EnumerateObject().ToDictionary(
                    statusResponse => int.Parse(statusResponse.Name, System.Globalization.CultureInfo.InvariantCulture),
                    statusResponse => statusResponse.Value.TryGetProperty("content", out var content)
                        ? content.EnumerateObject().Select(mediaType => mediaType.Name).ToArray()
                        : []))))
            .ToDictionary(operation => operation.Key, operation => operation.Responses);
    }
}
