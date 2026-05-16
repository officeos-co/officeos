
namespace OffceOs.Tests.Shared;

public static class HttpResponseFactory
{
    public static HttpResponseMessage SseResponse(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "text/event-stream"),
    };

    public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string content) => new(statusCode)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json"),
    };
}
