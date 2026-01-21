namespace AISPubSub;

public class AcknowledgementResponse
{
    public string? AcknowledgementId { get; set; }
    public string? Context { get; set; }
    public int TotalChunks { get; set; }
    public string? Datasource { get; set; }
}