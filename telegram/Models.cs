namespace Alga.telegram.Models;

public class UpdateRoot { public bool ok { get; set; } public List<UpdateResult>? result { get; set; } }

public class UpdateResult { public long? update_id { get; set; } public Message? message { get; set; } }

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

public class Chat
{
    public long id { get; set; }
    public string? type { get; set; }
    public string? title { get; set; }
    public string? username { get; set; }
    public string? first_name { get; set; }
    public string? last_name { get; set; }
}

public class MessageEntity
{
    public string? type { get; set; }
    public int offset { get; set; }
    public int length { get; set; }
    public string? url { get; set; }
    public User? user { get; set; }
    public string? language { get; set; }
}

public class PhotoSize
{
    public string? file_id { get; set; }
    public string? file_unique_id { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public int file_size { get; set; }
}

public class Video
{
    public string? file_id { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public int duration { get; set; }
    public string? mime_type { get; set; }
    public int file_size { get; set; }
}

public class Audio
{
    public string? file_id { get; set; }
    public int duration { get; set; }
    public string? performer { get; set; }
    public string? title { get; set; }
    public string? mime_type { get; set; }
    public int file_size { get; set; }
}

public class User
{
    public long id { get; set; }
    public bool? is_bot { get; set; }
    public string? first_name { get; set; }
    public string? last_name { get; set; }
    public string? username { get; set; }
    public string? language_code { get; set; }
    public bool? can_join_groups { get; set; }
}

public class DeleteMessageRoot
{
    public bool ok { get; set; }
    public bool result { get; set; }
    public int error_code { get; set; }
    public string? description { get; set; }
}

public class SendMessageResponseRoot
{
    public bool ok { get; set; }
    public Message? result { get; set; }
}

public class SendM
{
    public string? chat { get; set; }
    public string? text { get; set; }
    public string? file_url { get; set; }
    public string? file_type { get; set; }
    public int? reply_to_msg_id { get; set; }
}