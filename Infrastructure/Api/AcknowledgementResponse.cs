namespace AISPubSub.Infrastructure.Api;

public class AcknowledgementResponse
{
    public string? AcknowledgementId { get; set; }
    public string? Context { get; set; }
    public int TotalChunks { get; set; }
    public string? Datasource { get; set; }
    
    public string GetErrorMessage()
    {
        if (string.IsNullOrEmpty(Context)) return "Unknown Error";
        try 
        {
            // The context is a JSON string inside a JSON string
            using var doc = System.Text.Json.JsonDocument.Parse(Context);
            if (doc.RootElement.TryGetProperty("ErrorMessage", out var msg))
            {
                return msg.GetString() ?? "No message provided";
            }
        }
        catch { /* Not valid JSON or no ErrorMessage property */ }
        return Context; // Fallback to raw string
    }
}