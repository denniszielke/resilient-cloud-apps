namespace Contonance.WebPortal.Shared;

public class KnowledgeBaseResponse
{
    public string Question { get; set; }

    public string Answer { get; set; }
    public List<KBCitation> Citations { get; set; }
}

public class KBCitation
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string Filepath { get; set; }

    public string Url { get; set; }
}