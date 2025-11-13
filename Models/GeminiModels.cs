namespace vibium.Models;

public class GeminiRequest
{
    public List<Content>? contents { get; set; }
}

public class Content
{
    public List<Part>? parts { get; set; }
}

public class Part
{
    public string? text { get; set; }
}

public class GeminiResponse
{
    public List<Candidate>? candidates { get; set; }
}

public class Candidate
{
    public Content? content { get; set; }
}
