using System.Text.Json;
using System.Web;

namespace Alga.telegram;

public class Api
{
    public long id { get; }
    string url_root;
    readonly HttpClient httpClient;

    public Api(string token, IHttpClientFactory httpClientFactory)
    {
        this.id = long.Parse(token.Split(':')[0]);
        this.url_root = $"https://api.telegram.org/bot{token}";
        this.httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// Метод для получения обновлений
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    public async Task<Models.UpdateRoot?> GetUpdatesAsync(long? offset)
    {
        var url = new Uri($"{this.url_root}/getUpdates?offset={offset}");

        try
        {
            // Получаем строку с ответом и десериализуем её в объект
            var response = await this.httpClient.GetStringAsync(url);
            return JsonSerializer.Deserialize<Models.UpdateRoot>(response);
        }
        catch (HttpRequestException ex)
        {
            // Логируем ошибку или выполняем дополнительные действия
            // Например, можно вернуть null или какое-то дефолтное значение
            // Или логировать исключение
            //Console.Error.WriteLine($"Request failed: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            // Логируем ошибку десериализации
            //Console.Error.WriteLine($"Error deserializing response: {ex.Message}");
            return null;
        }

        // Models.UpdateRoot? m = null;
        // //try { 
        // m = JsonSerializer.Deserialize<Models.UpdateRoot>(await this.httpClient.GetStringAsync(this.url_root + "/getUpdates?offset=" + offset));
        // //}
        // //catch { }
        // return m;
    }
    
    public async Task<Models.DeleteMessageRoot?> DeleteMessageAsync(string chatId, int messageId)
    {
        var url = new Uri($"{this.url_root}/deleteMessage?chat_id={chatId}&message_id={messageId}");

        try
        {
            // Получаем строку с ответом
            var response = await this.httpClient.GetStringAsync(url);
            
            // Десериализуем строку в объект
            return JsonSerializer.Deserialize<Models.DeleteMessageRoot>(response);
        }
        catch (Exception ex)
        {
            // Логируем ошибку (например, можно использовать какой-то логгер)
            //Console.Error.WriteLine($"Error in delete_message: {ex.Message}");
            
            // Возвращаем null в случае ошибки
            return null;
        }

        // Models.DeleteMessageRoot? m = null;
        // try { m = JsonSerializer.Deserialize<Models.DeleteMessageRoot>(await this.httpClient.GetStringAsync(this.url_root + "/deleteMessage?chat_id=" + chat_id + "&message_id=" + message_id)); }
        // catch { }
        // return m;
    }

    public async Task<Models.SendMessageResponseRoot?> SendAsync(Models.SendM message)
    {
        try {
            message.text = string.IsNullOrEmpty(message.text) ? null : message.text;

            string? response = null;

            // Отправка текстового сообщения
            if (message.text != null && message.file_url == null)
            {
                response = await SendTextMessageAsync(message);
            }
            // Отправка файла (фото, видео, аудио)
            else if (message.file_url != null)
            {
                response = await SendFileMessageAsync(message);
            }

            // Десериализация ответа
            if (!string.IsNullOrEmpty(response))
            {
                return JsonSerializer.Deserialize<Models.SendMessageResponseRoot>(response);
            }
        }
        catch (Exception ex)
        {
            // Логируем ошибку для диагностики
            Console.Error.WriteLine($"Error sending message: {ex.Message}");
        }

        return null;

        // Models.SendMessageResponseRoot? m_return = null;

        // try
        // {
        //     if (string.IsNullOrEmpty(m.text))
        //         m.text = null;

        //     string? str = null;

        //     //var sterr = this.url_root + "/sendMessage?chat_id=" + m.chat + "&parse_mode=html&disable_web_page_preview=true" + "&text=" + HttpUtility.UrlEncode(m.text) + "&reply_to_msg_id=" + m.reply_to_msg_id;

        //     if (m.text != null && m.file_url == null)
        //     {
        //         var t = this.url_root + "/sendMessage?chat_id=" + m.chat + "&parse_mode=html&disable_web_page_preview=true" + "&text=" + HttpUtility.UrlEncode(m.text) + "&reply_to_msg_id=" + m.reply_to_msg_id;

        //         str = await this.httpClient.GetStringAsync(this.url_root + "/sendMessage?chat_id=" + m.chat + "&parse_mode=html&disable_web_page_preview=true" + "&text=" + HttpUtility.UrlEncode(m.text) + "&reply_to_message_id=" + m.reply_to_msg_id);
        //     }
        //     else if (m.file_url != null)
        //     {
        //         m.file_url = m.file_url.Trim();
        //         string? tp = null;
        //         string? name = null;
        //         byte[]? byte_array = null;
        //         if (m.file_url.Contains("http"))
        //         {
        //             byte_array = await this.httpClient.GetByteArrayAsync(m.file_url);
        //             //byte_array = await new proxy_cl.http_content(m.file_url).byte_array_async();
        //             // https://cdn4.telesco.pe/file/2f5b0b3e4c.mp4?token=FX69JFKis-gYH7ZLTjUR30ShMQyFSyOTT8lUsVSWaaH5TEBw-YzOa0HEcBKoEZG46E1NnaEIwMpZ569KZmXhHqBnrV91J84sJxEkSvhWh1WEncLZFL3n3kPYrN1rQbj9pO1dCXxz2Dq0bEWsS7SzZdiYrvaMJQWqtaByR8HoZdBjpuv_Rf_FrPHIwfTmNf7owuksevlY3AF6U22ytWtM0YzgZpA2YxF3dwBsU9RSMPj0sYaaarWOiL5vlbSiI-qqtzg1wvasz4exKO_5ohedAFJegl3EhNiTgH11U8yU0WXRxyrqyawl_fyIilBaUXsfVBsBT3_h3GBUAB8UNwEZew
        //             // https://cdn4.telesco.pe/file/A0r-kZUVDUXWpOdff7oAEwfzK_dNv9AzfTgXXzaewbyAS58lcUIrq7_lHBatmG-_w6n1sqTpcbAfg5gJekFew8UYOIdtVQ91u3z82cisLnd1TPCELPVZl-CyG3uJPzJf7nTHjEIrUrERFKSiNG8-etBHg86Tvm-sLGU8IjAE--TjHgcCovQO_KT_hay1dXyj-mrGqgcn1F9rFSvvoIf1C7EfmCpOPp_wftpPNPnyjSYK7AhRtEMup8NswNkuGzAdStuyDuudUoGFxJ-W2XejjiPl99J0-F00udzKMw8dfMaHE5JYgv6WD3ukePq2oqR8XEiRQOLMj4l6fC5VnQg_Fg.jpg
        //             var tp_split = m.file_url.Split('?');
        //             var tp_split_dot = tp_split[0].Split('.');
        //             tp = tp_split_dot[tp_split_dot.Length - 1];
        //             var tp_split_t = tp_split[0].Split('/');
        //             name = tp_split_t[tp_split_t.Length - 1];
        //         }
        //         else
        //         {
        //             if (File.Exists(m.file_url))
        //                 byte_array = File.ReadAllBytes(m.file_url);
        //             var tp_split = m.file_url.Split('.');
        //             tp = tp_split[tp_split.Length - 1];
        //             var tp_split_t = tp_split[0].Split('/');
        //             name = tp_split_t[tp_split_t.Length - 1];
        //         }

        //         string? tpe = null;
        //         if (tp == "jpg" || tp == "jpeg" || tp == "png")
        //             tpe = "photo";
        //         else if (tp == "mp4")
        //             tpe = "video";

        //         if (byte_array != null)
        //         {
        //             MultipartFormDataContent form = new MultipartFormDataContent();
        //             var streamContent = new StreamContent(new System.IO.MemoryStream(byte_array));
        //             streamContent.Headers.Add("Content-Type", "application/octet-stream");
        //             streamContent.Headers.Add("Content-Disposition", "form-data; name=\"" + tpe + "\"; filename=\"" + name + "\"");
        //             form.Add(streamContent, "file", name);

        //             string caption = "";
        //             if (m.text != null)
        //                 caption = "&caption=" + HttpUtility.UrlEncode(m.text);

        //             var action = "sendPhoto";
        //             if (tpe == "video")
        //                 action = "sendVideo";

        //             using (var response = await this.httpClient.PostAsync(this.url_root + "/" + action + "?chat_id=" + m.chat + caption, form))
        //             using (HttpContent content = response.Content)
        //                 str = await content.ReadAsStringAsync();

        //             //str = await this.httpClient
        //             //str = await new proxy_cl.http_content(this.url_root + "/" + action + "?chat_id=" + m.chat + caption).form_async(form);
        //         }
        //         else
        //         {
        //             if (m.file_url.Split('/').Length == 1)
        //                 if (m.file_type == "photo")
        //                     str = await this.httpClient.GetStringAsync(this.url_root + "/sendPhoto?chat_id=" + m.chat + "&photo=" + m.file_url + "&caption=" + HttpUtility.UrlEncode(m.text));
        //                 else if (m.file_type == "video")
        //                     str = await this.httpClient.GetStringAsync(this.url_root + "/sendVideo?chat_id=" + m.chat + "&video=" + m.file_url + "&caption=" + HttpUtility.UrlEncode(m.text));
        //                 else if (m.file_type == "audio")
        //                     str = await this.httpClient.GetStringAsync(this.url_root + "/sendAudio?chat_id=" + m.chat + "&audio=" + m.file_url + "&title=" + HttpUtility.UrlEncode(m.text));
        //         }

        //         if (byte_array != null && byte_array.Length < 100000)
        //             m.file_type = null;
        //     }

        //     if (!string.IsNullOrEmpty(str))
        //         return JsonSerializer.Deserialize<Models.SendMessageResponseRoot>(str); // Newtonsoft.Json.JsonConvert.DeserializeObject<m.send_message_response_root_json>(str);
        // }
        // catch { }

        // return m_return;
    }

    // Метод для отправки текстового сообщения
    async Task<string?> SendTextMessageAsync(Models.SendM message)
    {
        var url = $"{this.url_root}/sendMessage?chat_id={message.chat}&parse_mode=html&disable_web_page_preview=true&text={HttpUtility.UrlEncode(message.text)}&reply_to_message_id={message.reply_to_msg_id}";
        return await this.httpClient.GetStringAsync(url);
    }

    // Метод для отправки файла (фото, видео, аудио)
    async Task<string?> SendFileMessageAsync(Models.SendM message)
    {
        string? fileType = GetFileType(message.file_url);
        byte[]? fileBytes = await GetFileBytesAsync(message.file_url, fileType);

        if (fileBytes == null)
        {
            return null; // Если не удалось получить файл, возвращаем null
        }

        var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream(fileBytes));
        streamContent.Headers.Add("Content-Type", "application/octet-stream");
        streamContent.Headers.Add("Content-Disposition", $"form-data; name=\"{fileType}\"; filename=\"{GetFileName(message.file_url)}\"");
        form.Add(streamContent, "file", GetFileName(message.file_url));

        string caption = message.text != null ? "&caption=" + HttpUtility.UrlEncode(message.text) : string.Empty;
        string action = fileType == "video" ? "sendVideo" : "sendPhoto";

        var url = $"{this.url_root}/{action}?chat_id={message.chat}{caption}";
        var response = await this.httpClient.PostAsync(url, form);
        return await response.Content.ReadAsStringAsync();
    }

    // Метод для получения типа файла (photo, video, audio)
    string GetFileType(string fileUrl)
    {
        var extension = Path.GetExtension(fileUrl)?.ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" => "photo",
            ".mp4" => "video",
            ".mp3" or ".wav" => "audio",
            _ => "file" // Для других типов можно вернуть "file"
        };
    }

    // Метод для получения байтов из файла
    private async Task<byte[]?> GetFileBytesAsync(string fileUrl, string fileType)
    {
        if (Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
        {
            // Если это URL, загружаем файл по HTTP
            return await this.httpClient.GetByteArrayAsync(fileUrl);
        }
        else if (File.Exists(fileUrl))
        {
            // Если это локальный файл
            return File.ReadAllBytes(fileUrl);
        }
        return null;
    }

    // Метод для извлечения имени файла из URL
    private string GetFileName(string fileUrl)
    {
        return Path.GetFileName(fileUrl);
    }

    public class user_inf
    {
        public string? username(Models.Message message)
        {
            string? user_name = null;

            if (message.from != null && !String.IsNullOrEmpty(message.from.username))
                user_name = "@" + message.from.username;

            return user_name;
        }

        public string? user_short(Models.Message message)
        {
            string? user_name = null;

            if (message.from != null && !String.IsNullOrEmpty(message.from.first_name))
                user_name = message.from.first_name;
            if (message.from != null && !String.IsNullOrEmpty(message.from.last_name))
            {
                if (string.IsNullOrEmpty(user_name))
                    user_name = message.from.last_name;
                else
                    user_name += " " + message.from.last_name;
            }

            if (message.from != null && !String.IsNullOrEmpty(message.from.username))
                user_name = "@" + message.from.username;

            return user_name;
        }
    }

    public class send_m_optimization
    {
        public void set(Models.SendM m, bool short_title = false, bool hide_links = false)
        {
            if (m.text != null && m.text.Length > 0)
            {
                if (hide_links)
                {
                    var title_split = m.text.Split(' ');
                    int link_n = 0;
                    for (var i = 0; i < title_split.Length; i++)
                        if (title_split[i].Contains("http") && title_split[i][0] == 'h')
                        {
                            title_split[i] = "<a href=\"" + title_split[i] + "\">[ .:. ]</a>";
                            link_n++;
                        }
                    if (link_n > 0)
                    {
                        m.text = null;
                        foreach (var i in title_split)
                            m.text += i + " ";
                        if (m.text != null)
                            m.text = m.text.Substring(0, m.text.Length - 1);
                    }
                }

                if (short_title && m.text != null && m.text.Length > 512)
                {
                    var n = 0;
                    var n_sub = 0;
                    var text_split = m.text.Split(' ');
                    for (var i = 0; i < text_split.Length; i++)
                        if (n < 512)
                        {
                            n += text_split[i].Length + 1;
                            if (text_split[i].Length > 1 && text_split[i][0] == '<' && text_split[i][1] == 'a')
                                n_sub += 7;
                            else
                                n_sub += text_split[i].Length + 1;
                        }
                        else
                            break;
                    m.text = m.text.Substring(0, n - 1);
                    if (n_sub > 128)
                        m.text += " ...";
                }
            }
        }
    }
}