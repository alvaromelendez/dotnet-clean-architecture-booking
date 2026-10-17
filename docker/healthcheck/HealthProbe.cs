// Container health probe for the chiseled runtime image, which has no shell, curl or wget.
// It runs on the .NET runtime already in the image: `dotnet /healthcheck/HealthProbe.dll <url>`.
// Exit code 0 when <url> answers 2xx within the timeout, 1 otherwise (the Docker HEALTHCHECK contract).
#:property PublishAot=false
#:property InvariantGlobalization=true

var url = args.Length > 0 ? args[0] : "http://localhost:8080/health/ready";

try
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    using var response = await client.GetAsync(url);

    return response.IsSuccessStatusCode ? 0 : 1;
}
catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
{
    return 1;
}
