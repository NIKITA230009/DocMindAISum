namespace DocMind.Core.Dto;

public class ExecuteCommandRequest
{
    public string Text { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
}