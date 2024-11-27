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