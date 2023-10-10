public class Citation
{
    public string? content { get; set; }
    public object? id { get; set; }
    public string? title { get; set; }
    public string? filepath { get; set; }
    public object? url { get; set; }
    public Metadata? metadata { get; set; }
    public string? chunk_id { get; set; }
}

public class Metadata
{
    public string? chunking { get; set; }
}

public class ChatExtensionContextMessage
{
    public ChatExtensionContextMessage()
    {
        citations = new();
    }

    public List<Citation> citations { get; set; }
    public string? intent { get; set; }
}