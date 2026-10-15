using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BookingSystem.Api.Contracts.Bookings;
using BookingSystem.Api.Contracts.Common;
using BookingSystem.Api.Contracts.Rooms;
using Shouldly;

namespace BookingSystem.IntegrationTests.Api;

/// <summary>
/// Thin helpers over <see cref="HttpClient"/> using the API's own contracts and JSON conventions.
/// </summary>
internal static class ApiTestClient
{
    public const string ProblemJson = "application/problem+json";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<Guid> CreateRoomAsync(this HttpClient client, int capacity = 10, decimal hourlyRate = 20m)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/rooms",
            new CreateRoomRequest($"Room {Guid.NewGuid():N}", capacity, hourlyRate, "USD"),
            Json);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<ResourceCreatedResponse>(Json))!.Id;
    }

    public static Task<HttpResponseMessage> PostBookingAsync(
        this HttpClient client,
        Guid roomId,
        DateTimeOffset start,
        DateTimeOffset end,
        int attendees = 2,
        Guid? userId = null) =>
        client.PostAsJsonAsync(
            "/api/v1/bookings",
            new CreateBookingRequest(roomId, userId ?? Guid.NewGuid(), start, end, attendees),
            Json);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    /// <summary>
    /// Asserts an RFC 9457 problem response and returns its JSON for further checks.
    /// </summary>
    public static async Task<JsonElement> ShouldBeProblemAsync(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string? expectedCode = null)
    {
        response.StatusCode.ShouldBe(expectedStatus);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemJson);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("status").GetInt32().ShouldBe((int)expectedStatus);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();

        var code = problem.GetProperty("code").GetString();
        code.ShouldNotBeNullOrWhiteSpace();
        if (expectedCode is not null)
        {
            code.ShouldBe(expectedCode);
        }

        return problem;
    }
}
