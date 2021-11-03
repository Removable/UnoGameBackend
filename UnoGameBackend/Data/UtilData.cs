namespace UnoGameBackend.Data;

public class Result
{
    public bool Success { get; set; }

    public string Msg { get; set; } = string.Empty;
    
    public object? Data { get; set; }
}