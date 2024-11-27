namespace Alga.telegram.Models;
public class Audio
{
    public string? file_id { get; set; }
    public int duration { get; set; }
    public string? performer { get; set; }
    public string? title { get; set; }
    public string? mime_type { get; set; }
    public int file_size { get; set; }
}
