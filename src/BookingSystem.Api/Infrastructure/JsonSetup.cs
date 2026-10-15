using System.Text.Json.Serialization;

namespace BookingSystem.Api.Infrastructure;

internal static class JsonSetup
{
    /// <summary>
    /// camelCase (web defaults), enums as strings and no <c>null</c> properties in responses.
    /// </summary>
    public static IServiceCollection AddApiJson(this IServiceCollection services) =>
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
}
