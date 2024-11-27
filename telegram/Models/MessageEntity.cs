namespace Alga.telegram.Models;
public class MessageEntity
{
    public string? type { get; set; }
    public int offset { get; set; }
    public int length { get; set; }
    public string? url { get; set; }
    public User? user { get; set; }
    public string? language { get; set; }
}
