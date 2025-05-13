using System.Text.Json.Serialization;

namespace Alga.telegram.Models;

public class ReplyMarkup {
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<List<InlineKeyboardButton>>? inline_keyboard { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<List<KeyboardButton>>? keyboard { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? resize_keyboard { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? one_time_keyboard { get; set; }
}