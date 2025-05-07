namespace Alga.telegram.Models;
public class InlineKeyboardButton {
    public string? text { get; set; }
    public WebAppInfo? web_app { get; set; }
    public string? url { get; set; }
    
    // Можно добавить другие типы кнопок (url, callback_data и т.д.)
}
