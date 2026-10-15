using Asp.Versioning;

namespace BookingSystem.Api.Infrastructure;

/// <summary>
/// Supported API versions. To introduce v2: declare it here, map a second versioned group in
/// <see cref="ApiVersioningSetup.MapVersionedEndpoints"/> and register the v2 endpoints on it.
/// </summary>
internal static class ApiVersions
{
    public static readonly ApiVersion V1 = new(1);
}
