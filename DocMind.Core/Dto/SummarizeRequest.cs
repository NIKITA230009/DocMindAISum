namespace DocMind.Core.Dto;

public class SummarizeRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Prompt { get; set; }
}