using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Web;

namespace Alga.telegram;

/// <summary>
/// Represents the Telegram Bot API client for interacting with Telegram servers.
/// </summary>
public class Api
{
    readonly ILogger? logger;
    readonly HttpClient httpClient;
    readonly string UrlRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="Api"/> class.
    /// </summary>
    /// <param name="token">Telegram bot token for authentication.</param>
    /// <param name="httpClientFactory">Factory to create <see cref="HttpClient"/> instances.</param>
    /// <param name="loggerFactory">Optional logger factory for logging.</param>
    public Api(string token, IHttpClientFactory httpClientFactory, ILoggerFactory? loggerFactory = null)
    {
        logger = loggerFactory?.CreateLogger<Api>();
        UrlRoot = $"https://api.telegram.org/bot{token}";
        this.httpClient = httpClientFactory.CreateClient();

        logger?.LogInformation("Telegram API initialized.");
    }

    /// <summary>
    /// Receiving updates from the Telegram server.
    /// </summary>
    /// <param name="offset">The offset for fetching updates.</param>
    /// <returns>A deserialized <see cref="Models.UpdateRoot"/> object containing updates, or null if the request fails.</returns>
    public async Task<Models.UpdateRoot?> GetUpdatesAsync(long? offset) => await SendGetRequestAsync<Models.UpdateRoot>($"{UrlRoot}/getUpdates?offset={offset}");
    
    /// <summary>
    /// Deletes a specific message in a chat.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat where the message is located.</param>
    /// <param name="messageId">The unique identifier of the message to be deleted.</param>
    /// <returns>A deserialized <see cref="Models.DeleteMessageRoot"/> object, or null if the request fails.</returns>
    public async Task<Models.DeleteMessageRoot?> DeleteMessageAsync(string chatId, int messageId) => await SendGetRequestAsync<Models.DeleteMessageRoot>($"{UrlRoot}/deleteMessage?chat_id={chatId}&message_id={messageId}");

    /// <summary>
    /// Sends a message (text or media) using the Telegram API.
    /// </summary>
    /// <param name="message">The message object containing content and metadata.</param>
    /// <returns>A deserialized <see cref="Models.SendMessageResponseRoot"/> object, or null if the request fails.</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Models.SendMessageResponseRoot?> SendMessageAsync(Models.SendM message) {
        var mn = $"{nameof(SendMessageAsync)}()";
        try {
            string? response = message switch {
                { text: not null, file_url: null } => await SendTextMessageAsync(message),
                { file_url: not null } => await SendFileMessageAsync(message),
                _ => throw new ArgumentException($"{mn} Invalid message data")
            };
            return !string.IsNullOrEmpty(response) ? JsonSerializer.Deserialize<Models.SendMessageResponseRoot>(response) : null;
        } catch (Exception ex) { logger?.LogError(ex, $"{mn} Error sending message"); return null; }
    }

    /// <summary>
    /// Sends a text message using the Telegram API.
    /// </summary>
    /// <param name="message">The message object containing text and metadata.</param>
    /// <returns>The raw response from the API as a string, or null if the request fails.</returns>
    //async Task<string?> SendTextMessageAsync(Models.SendM message) => await SendGetRequestAsync<string?>($"{UrlRoot}/sendMessage?chat_id={message.chat}&parse_mode=html&disable_web_page_preview=true&text={HttpUtility.UrlEncode(message.text)}&reply_to_message_id={message.reply_to_msg_id}");

private async Task<string?> SendTextMessageAsync(Models.SendM message)
{
    var payload = new Dictionary<string, object?>
    {
        ["chat_id"] = message.chat,
        ["text"] = message.text,
        ["parse_mode"] = "html",
        ["disable_web_page_preview"] = true
    };

    if (message.reply_to_msg_id.HasValue)
        payload["reply_to_message_id"] = message.reply_to_msg_id;

    if (message.reply_markup != null)
    {
        payload["reply_markup"] = message.reply_markup;
    }

    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    var content = new StringContent(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json");

    using var client = new HttpClient();
    var response = await client.PostAsync($"{UrlRoot}/sendMessage", content);
    return await response.Content.ReadAsStringAsync();
}


    /// <summary>
    /// Sends a file message (photo, video, audio) using the Telegram API.
    /// </summary>
    /// <param name="message">The message object containing file URL and metadata.</param>
    /// <returns>The raw response from the API as a string, or null if the request fails.</returns>
    async Task<string?> SendFileMessageAsync(Models.SendM message) {
        var mn = $"{nameof(SendGetRequestAsync)}()";

        if(string.IsNullOrEmpty(message.file_url)) { logger?.LogError($"{mn} SendFileMessageAsync() - File url not defined"); return null; }

        // Determining the file type (photo, video, audio)
        var fileType = Path.GetExtension(message.file_url)?.ToLower() switch
        {
            ".jpg" or ".jpeg" or ".png" => "photo",
            ".mp4" => "video",
            ".mp3" or ".wav" => "audio",
            _ => null
        };
        if (fileType == null) { logger?.LogError("SendFileMessageAsync() - Unsupported file type"); return null;}

        // Getting the contents of the file as bytes
        byte[]? fileBytes = Uri.IsWellFormedUriString(message.file_url, UriKind.Absolute)
            ? await httpClient.GetByteArrayAsync(message.file_url)
            : File.Exists(message.file_url)
                ? await File.ReadAllBytesAsync(message.file_url) : null;
        if (fileBytes == null) { logger?.LogError("SendFileMessageAsync() - Failed to retrieve file bytes."); return null; }

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

        return await SendPostRequestAsync<string?>($"{UrlRoot}/{action}?chat_id={message.chat}{caption}", form);
    }

    /// <summary>
    /// Sends a GET request and deserializes the response.
    /// </summary>
    /// <typeparam name="T">The type of the expected response.</typeparam>
    /// <param name="url">The URL of the GET request.</param>
    /// <returns>The deserialized response, or default if the request fails.</returns>
    async Task<T?> SendGetRequestAsync<T>(string url) {
        var mn = $"{nameof(SendGetRequestAsync)}()";
        try {
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync());
            logger?.LogError($"{mn} Request failed with status code: {response.StatusCode}. url: " + url);
        } catch (Exception ex) { logger?.LogError(ex, $"{mn} Error during GET request"); }
        return default;
    }

    /// <summary>
    /// Sends a POST request with content and deserializes the response.
    /// </summary>
    /// <typeparam name="T">The type of the expected response.</typeparam>
    /// <param name="url">The URL of the POST request.</param>
    /// <param name="content">The HTTP content to send.</param>
    /// <returns>The deserialized response, or default if the request fails.</returns>
    private async Task<T?> SendPostRequestAsync<T>(string url, HttpContent content) {
        var mn = $"{nameof(SendPostRequestAsync)}()";
        try {
            var response = await httpClient.PostAsync(url, content).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync());
            logger?.LogError($"{mn} Request failed with status code: {response.IsSuccessStatusCode}", response.StatusCode);
        } catch (Exception ex) { logger?.LogError(ex, $"{mn} Error during POST request"); }

        return default;
    }
}