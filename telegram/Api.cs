using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Web;
using System.Reflection;

namespace Alga.telegram;

/// <summary>
/// Represents the Telegram Bot API client for interacting with Telegram servers.
/// </summary>
public class Api
{
    readonly ILogger? logger;
    readonly HttpClient httpClient;
    readonly string url_root;

    /// <summary>
    /// Initializes a new instance of the <see cref="Api"/> class.
    /// </summary>
    /// <param name="token">Telegram bot token for authentication.</param>
    /// <param name="httpClientFactory">Factory to create <see cref="HttpClient"/> instances.</param>
    /// <param name="loggerFactory">Optional logger factory for logging.</param>
    public Api(string token, IHttpClientFactory httpClientFactory, ILoggerFactory? loggerFactory = null)
    {
        logger = loggerFactory?.CreateLogger<Api>();
        this.url_root = $"https://api.telegram.org/bot{token}";
        this.httpClient = httpClientFactory.CreateClient();

        logger?.LogInformation("Telegram API initialized.");
    }

    /// <summary>
    /// Receiving updates from the Telegram server.
    /// </summary>
    /// <param name="offset">The offset for fetching updates.</param>
    /// <returns>A deserialized <see cref="Models.UpdateRoot"/> object containing updates, or null if the request fails.</returns>
    public async Task<Models.UpdateRoot?> GetUpdatesAsync(long? offset) {
        var content = await ExecuteGetRequestAsync($"{this.url_root}/getUpdates?offset={offset}", MethodBase.GetCurrentMethod());
        if(!string.IsNullOrEmpty(content)) return JsonSerializer.Deserialize<Models.UpdateRoot>(content);
        return null;
    }
    
    /// <summary>
    /// Deletes a specific message in a chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat where the message is located.</param>
    /// <param name="messageId">The unique identifier of the message to be deleted.</param>
    /// <returns>A deserialized <see cref="Models.DeleteMessageRoot"/> object, or null if the request fails.</returns>
    public async Task<Models.DeleteMessageRoot?> DeleteMessageAsync(string chatId, int messageId) { 
        var content = await ExecuteGetRequestAsync($"{this.url_root}/deleteMessage?chat_id={chatId}&message_id={messageId}", MethodBase.GetCurrentMethod());
        if(!string.IsNullOrEmpty(content)) return JsonSerializer.Deserialize<Models.DeleteMessageRoot>(content);
        return null;
    }

    /// <summary>
    /// Sends a message (text or media) using the Telegram API.
    /// </summary>
    /// <param name="message">The message object containing content and metadata.</param>
    /// <returns>A deserialized <see cref="Models.SendMessageResponseRoot"/> object, or null if the request fails.</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Models.SendMessageResponseRoot?> SendMessageAsync(Models.SendM message)
    {
        try {
            string? response = message switch
            {
                { text: not null, file_url: null } => await SendTextMessageAsync(message),
                { file_url: not null } => await SendFileMessageAsync(message),
                _ => throw new ArgumentException("Invalid message data")
            };

            return !string.IsNullOrEmpty(response)
                ? JsonSerializer.Deserialize<Models.SendMessageResponseRoot>(response)
                : null;
        }
        catch (Exception ex) { logger?.LogError(ex, "Error sending message"); }

        return null;
    }

    /// <summary>
    /// Sends a text message using the Telegram API.
    /// </summary>
    /// <param name="message">The message object containing text and metadata.</param>
    /// <returns>The raw response from the API as a string, or null if the request fails.</returns>
    async Task<string?> SendTextMessageAsync(Models.SendM message)
    {
        var methodName = MethodBase.GetCurrentMethod()?.Name;

        var url = $"{url_root}/sendMessage" +
                  $"?chat_id={message.chat}" +
                  $"&parse_mode=html" +
                  $"&disable_web_page_preview=true" +
                  $"&text={HttpUtility.UrlEncode(message.text)}" +
                  $"&reply_to_message_id={message.reply_to_msg_id}";

        return await ExecuteGetRequestAsync(url, MethodBase.GetCurrentMethod());
    }

    /// <summary>
    /// Sends a file message (photo, video, audio) using the Telegram API.
    /// </summary>
    /// <param name="message">The message object containing file URL and metadata.</param>
    /// <returns>The raw response from the API as a string, or null if the request fails.</returns>
    async Task<string?> SendFileMessageAsync(Models.SendM message)
    {
        if(String.IsNullOrEmpty(message.file_url)) {
            logger?.LogError("SendFileMessageAsync() - File url not defined");
            return null;
        }

        // Determining the file type (photo, video, audio)
        var fileType = Path.GetExtension(message.file_url)?.ToLower() switch
        {
            ".jpg" or ".jpeg" or ".png" => "photo",
            ".mp4" => "video",
            ".mp3" or ".wav" => "audio",
            _ => null
        };

        // Getting the contents of the file as bytes
        var fileBytes = Uri.IsWellFormedUriString(message.file_url, UriKind.Absolute)
            ? await httpClient.GetByteArrayAsync(message.file_url)
            : File.Exists(message.file_url)
                ? await Task.FromResult(File.ReadAllBytes(message.file_url)) : null;


        if (fileBytes == null) {
            logger?.LogError("SendFileMessageAsync() - Failed to retrieve file bytes.");
            return null;
        }

        // Get file name
        string fileName = Path.GetFileName(message.file_url);

        // Creating the form content
        using var form = new MultipartFormDataContent();
        using var stream = new MemoryStream(fileBytes);
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        streamContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data") { Name = fileType, FileName = fileName };
        form.Add(streamContent, "file", fileName);

        // Forming a URL
        string caption = !string.IsNullOrEmpty(message.text) 
            ? $"&caption={HttpUtility.UrlEncode(message.text)}" 
            : string.Empty;
        string action = fileType == "video" ? "sendVideo" : "sendPhoto";
        var url = $"{url_root}/{action}?chat_id={message.chat}{caption}";

        return await ExecutePostRequestAsync(url, form, MethodBase.GetCurrentMethod());
    }

    /// <summary>
    /// Executes a GET request to the specified URL and returns the response as a string.
    /// </summary>
    /// <param name="url">The URL to send the GET request to.</param>
    /// <param name="methodBase">The method metadata used for logging (optional).</param>
    /// <returns>A string containing the response content if the request is successful; otherwise, null if the request fails or an exception occurs.</returns>
    private async Task<string?> ExecuteGetRequestAsync(string url, MethodBase? methodBase)
    {
        try {
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync();

            logger?.LogError($"{methodBase?.Name}() - Request failed with status code: {response.StatusCode}");
        }
        catch (Exception ex) { logger?.LogError($"{methodBase?.Name}() - Error: {ex.Message}"); }

        return null;
    }

    /// <summary>
    /// Executes a POST request to the specified URL with the provided content and returns the response as a string.
    /// </summary>
    /// <param name="url">The URL to send the POST request to.</param>
    /// <param name="content">The HTTP content to include in the POST request body.</param>
    /// <param name="methodBase">The method metadata used for logging (optional).</param>
    /// <returns>
    /// A string containing the response content if the request is successful;
    /// otherwise, null if the request fails or an exception occurs.
    /// </returns>
    private async Task<string?> ExecutePostRequestAsync(string url, HttpContent content, MethodBase? methodBase)
    {
        try {
            var response = await httpClient.PostAsync(url, content).ConfigureAwait(false);
            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync();
            logger?.LogError($"{methodBase?.Name}() - Request failed with status code: {response.StatusCode}");
        }
        catch (Exception ex) { logger?.LogError($"{methodBase?.Name}() - Error: {ex.Message}"); }

        return null;
    }
}

// 162 / 177 / 202


    // public class user_inf
    // {
    //     public string? username(Models.Message message)
    //     {
    //         string? user_name = null;

    //         if (message.from != null && !String.IsNullOrEmpty(message.from.username))
    //             user_name = "@" + message.from.username;

    //         return user_name;
    //     }

    //     public string? user_short(Models.Message message)
    //     {
    //         string? user_name = null;

    //         if (message.from != null && !String.IsNullOrEmpty(message.from.first_name))
    //             user_name = message.from.first_name;
    //         if (message.from != null && !String.IsNullOrEmpty(message.from.last_name))
    //         {
    //             if (string.IsNullOrEmpty(user_name))
    //                 user_name = message.from.last_name;
    //             else
    //                 user_name += " " + message.from.last_name;
    //         }

    //         if (message.from != null && !String.IsNullOrEmpty(message.from.username))
    //             user_name = "@" + message.from.username;

    //         return user_name;
    //     }
    // }

    // public class send_m_optimization
    // {
    //     public void set(Models.SendM m, bool short_title = false, bool hide_links = false)
    //     {
    //         if (m.text != null && m.text.Length > 0)
    //         {
    //             if (hide_links)
    //             {
    //                 var title_split = m.text.Split(' ');
    //                 int link_n = 0;
    //                 for (var i = 0; i < title_split.Length; i++)
    //                     if (title_split[i].Contains("http") && title_split[i][0] == 'h')
    //                     {
    //                         title_split[i] = "<a href=\"" + title_split[i] + "\">[ .:. ]</a>";
    //                         link_n++;
    //                     }
    //                 if (link_n > 0)
    //                 {
    //                     m.text = null;
    //                     foreach (var i in title_split)
    //                         m.text += i + " ";
    //                     if (m.text != null)
    //                         m.text = m.text.Substring(0, m.text.Length - 1);
    //                 }
    //             }

    //             if (short_title && m.text != null && m.text.Length > 512)
    //             {
    //                 var n = 0;
    //                 var n_sub = 0;
    //                 var text_split = m.text.Split(' ');
    //                 for (var i = 0; i < text_split.Length; i++)
    //                     if (n < 512)
    //                     {
    //                         n += text_split[i].Length + 1;
    //                         if (text_split[i].Length > 1 && text_split[i][0] == '<' && text_split[i][1] == 'a')
    //                             n_sub += 7;
    //                         else
    //                             n_sub += text_split[i].Length + 1;
    //                     }
    //                     else
    //                         break;
    //                 m.text = m.text.Substring(0, n - 1);
    //                 if (n_sub > 128)
    //                     m.text += " ...";
    //             }
    //         }
    //     }
    // }


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