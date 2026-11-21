namespace OnlineShop.Api.Middleware;

public class ApiError
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}