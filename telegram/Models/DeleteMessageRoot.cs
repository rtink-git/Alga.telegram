namespace Alga.telegram.Models;
public class DeleteMessageRoot
{
    public bool ok { get; set; }
    public bool result { get; set; }
    public int error_code { get; set; }
    public string? description { get; set; }
}
