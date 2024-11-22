# Alga.telegram

Represents the Telegram Bot API [Telegram Bot API](https://core.telegram.org/bots/api) (HTTP-based interface created for developers keen on building bots for Telegram) client for interacting with Telegram servers.

## How does this work. Step by step

1. Looking for [@BotFather - bot](https://t.me/BotFather) and create a new bot and configure it. And finally you will receive a unique token for your bot

2. Code example

```
const string token = "1111111111:AAAAAABBBBBBCCCCCCDDDDDDEEEEEEXXXXXX";
long? bot_offset = 0;

var bot = new Alga.telegram.Api(token, httpClientFactory);

while (true) {
    await Task.Delay(1000);

    Alga.telegram.Models.UpdateRoot? updates = await bot.GetUpdatesAsync(bot_offset);

    if (updates != null && updates.ok == true && updates.result != null) {
        foreach (var update in updates.result) {
            var message = update.message;
            if (message != null) {
                if (message.text == "/start" && message.chat != null) // команда работает только внутри бота
                try
                {
                    var send = await bot.SendMessageAsync(new Alga.telegram.Models.SendM() { chat = message.chat.id.ToString(), text = "<b>Welcome.</b> Thank you for being with us." });
                    if (send != null && send.ok) Logger.LogInformation(logMainPart + message.chat.id + ". command: " + start_cmd);
                        else Logger.LogError(logMainPart + message.chat.id + ". command: " + start_cmd);
                }
                catch { Logger.LogCritical(logMainPart + message.chat.id + ". command: " + start_cmd + ". error: telegram library"); }

                bot_offset = update.update_id + 1;
            }
        }
    }
}
```