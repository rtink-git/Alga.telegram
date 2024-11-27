namespace Alga.telegram.Models;
public class SendM
{
    public string? chat { get; set; }
    public string? text { get; set; }
    public string? file_url { get; set; }
    public string? file_type { get; set; }
    public int? reply_to_msg_id { get; set; }
}
