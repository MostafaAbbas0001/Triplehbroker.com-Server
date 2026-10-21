using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebsiteAPI.Logging;

namespace WebsiteAPI.Middleware;

public sealed class ApiRequestLoggingMiddleware(
    RequestDelegate next,
    ILoggerFactory loggerFactory)
{
    private const int MaximumLoggedBodyLength = 16_384;
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordHash",
        "token",
        "accessToken",
        "refreshToken",
        "authorization",
        "secret",
        "jwt"
    };

    private readonly ILogger logger = loggerFactory.CreateLogger("HTTP");

    public async Task InvokeAsync(HttpContext context, RequestLogContext requestLog)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var request = await ReadRequestAsync(context.Request);
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        try
        {
            await next(context);
        }
        catch (Exception caught)
        {
            exception = caught;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = exception is null
                ? context.Response.StatusCode
                : StatusCodes.Status500InternalServerError;
            var queries = requestLog.GetQueries();
            var queryText = queries.Count == 0
                ? "  (none)"
                : string.Join(Environment.NewLine, queries.Select((query, index) => $"  {index + 1}. {query}"));

            logger.LogInformation(
                "{Method} {Path} -> {StatusCode} ({ElapsedMilliseconds} ms)\nRequest: {Request}\nQueries:\n{Queries}",
                context.Request.Method.ToUpperInvariant(),
                context.Request.Path.Value,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                request,
                queryText);
        }
    }

    private static async Task<string> ReadRequestAsync(HttpRequest request)
    {
        var query = request.QueryString.HasValue ? request.QueryString.Value : null;
        if (request.ContentLength is null or 0)
        {
            return query ?? "(none)";
        }

        if (request.ContentLength > MaximumLoggedBodyLength)
        {
            return $"[body omitted: {request.ContentLength} bytes]";
        }

        request.EnableBuffering();
        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return RedactJson(body);
    }

    private static string RedactJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "(empty)";
        }

        try
        {
            var node = JsonNode.Parse(body);
            Redact(node);
            return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "null";
        }
        catch (JsonException)
        {
            return "[non-JSON body omitted]";
        }
    }

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToArray())
            {
                if (SensitiveProperties.Contains(property.Key))
                {
                    jsonObject[property.Key] = "[REDACTED]";
                }
                else
                {
                    Redact(property.Value);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                Redact(item);
            }
        }
    }
}
