using System.Text.Json;

namespace OnlineShop.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _log;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var original = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Unhandled exception for {Method} {Path} (trace {TraceId})",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);

            buffer.SetLength(0);
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";

            var error = new ApiError
            {
                Status = 500,
                Message = ErrorMessages.For(500),
                TraceId = context.TraceIdentifier
            };
            await buffer.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(error, Json));
        }

        if (context.Response.StatusCode >= 400)
            await ReshapeAsync(context, buffer);

        buffer.Position = 0;
        context.Response.Body = original;
        context.Response.ContentLength = buffer.Length;
        await buffer.CopyToAsync(original);
    }

    private static async Task ReshapeAsync(HttpContext context, MemoryStream buffer)
    {
        var status = context.Response.StatusCode;
        var raw = buffer.Length == 0 ? string.Empty : System.Text.Encoding.UTF8.GetString(buffer.ToArray());

        var contentType = context.Response.ContentType ?? string.Empty;
        var isJson = contentType.Contains("json", StringComparison.OrdinalIgnoreCase);

        Dictionary<string, JsonElement>? body = null;
        if (isJson && raw.Length > 0)
        {
            try { body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(raw, Json); }
            catch (JsonException) { body = null; }
        }

        // already standard: has status and message
        if (body is not null && body.ContainsKey("status") && body.ContainsKey("message"))
            return;

        var result = new Dictionary<string, object?>();
        string? message = null;
        Dictionary<string, string[]>? errors = null;

        if (body is not null)
        {
            //validation of problem details
            if (body.TryGetValue("errors", out var errEl) && errEl.ValueKind == JsonValueKind.Object)
            {
                errors = new Dictionary<string, string[]>();
                foreach (var prop in errEl.EnumerateObject())
                    errors[prop.Name] = prop.Value.ValueKind == JsonValueKind.Array
                        ? prop.Value.EnumerateArray().Select(x => x.GetString() ?? "").ToArray()
                        : new[] { prop.Value.ToString() };

                message = errors.Values.SelectMany(v => v).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            }

            if (body.TryGetValue("message", out var m) && m.ValueKind == JsonValueKind.String)
                message = m.GetString();

            // keep every extra field (requiresConfirm, problems, orderIds, code ...)
            foreach (var (key, value) in body)
            {
                if (key is "message" or "status" or "errors" or "title" or "type" or "traceId") continue;
                result[key] = value;
            }
        }

        result["status"] = status;
        result["message"] = string.IsNullOrWhiteSpace(message) ? ErrorMessages.For(status) : message;
        if (errors is not null) result["errors"] = errors;
        result["traceId"] = context.TraceIdentifier;

        var bytes = JsonSerializer.SerializeToUtf8Bytes(result, Json);
        buffer.SetLength(0);
        await buffer.WriteAsync(bytes);
        context.Response.ContentType = "application/json; charset=utf-8";
    }
}