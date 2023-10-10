namespace Contonance.WebPortal.Shared;

#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.

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

#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
