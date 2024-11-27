namespace Alga.telegram.Models;
public class Message
{
    public int message_id { get; set; }
    public User? from { get; set; }
    public Chat? sender_chat { get; set; }
    public Chat? chat { get; set; }
    public int date { get; set; }
    public string? text { get; set; }
    public MessageEntity[]? entities { get; set; }
    public string? caption { get; set; }
    public PhotoSize[]? photo { get; set; }
    public Video? video { get; set; }
    public Audio? audio { get; set; }
    public User? new_chat_member { get; set; }
    public String? new_chat_title { get; set; }
    public User? left_chat_member { get; set; }
    public Message? reply_to_message { get; set; }
}
