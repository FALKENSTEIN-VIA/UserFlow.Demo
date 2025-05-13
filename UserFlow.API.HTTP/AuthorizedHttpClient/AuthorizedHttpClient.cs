using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UserFlow.API.Http.Auth;
using UserFlow.API.HTTP.Services.Interfaces;

namespace UserFlow.API.HTTP;

/// <summary>
/// 🌐 HTTP wrapper that automatically injects the JWT token and refreshes it if necessary.
/// </summary>
public class AuthorizedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ITokenRefreshService _tokenRefreshService;
    private readonly ISecureTokenStore _tokenStore;
    private readonly ILogger<AuthorizedHttpClient> _logger;

    public AuthorizedHttpClient(
        HttpClient httpClient,
        ITokenRefreshService tokenRefreshService,
        ISecureTokenStore tokenStore,
        ILogger<AuthorizedHttpClient> logger)
    {
        _httpClient = httpClient;
        _tokenRefreshService = tokenRefreshService;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    #region 📥 GET

    public async Task<T?> GetAsync<T>(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await SendAsync(request);
        return await HandleResponse<T>(response);
    }

    public async Task<HttpResponseMessage> GetRawAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = await _tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _httpClient.SendAsync(request);
    }

    #endregion

    #region 📤 POST

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var request = BuildJsonRequest(HttpMethod.Post, url, data);
        var response = await SendAsync(request);
        return await HandleResponse<TResponse>(response);
    }

    public async Task<HttpResponseMessage> PostAsync<TRequest>(string url, TRequest data)
    {
        var request = BuildJsonRequest(HttpMethod.Post, url, data);
        return await SendAsync(request);
    }

    #endregion

    #region 📥 PUT

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var request = BuildJsonRequest(HttpMethod.Put, url, data);
        var response = await SendAsync(request);
        return await HandleResponse<TResponse>(response);
    }

    public async Task<HttpResponseMessage> PutAsync<TRequest>(string url, TRequest data)
    {
        var request = BuildJsonRequest(HttpMethod.Put, url, data);
        return await SendAsync(request);
    }

    #endregion

    #region ❌ DELETE

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        return await SendAsync(request);
    }

    #endregion

    #region 🧠 Helpers

    private HttpRequestMessage BuildJsonRequest<TRequest>(HttpMethod method, string url, TRequest data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return new HttpRequestMessage(method, url) { Content = content };
    }

    private async Task<T?> HandleResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("❌ API responded with status code {StatusCode}", response.StatusCode);
            return default;
        }

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            var res = JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);
            if (res == null)
                _logger.LogWarning("❌ Deserialized response is null. JSON: {Json}", json);

            return res;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON deserialization error. Raw response: {Json}", json);
            throw;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("⚠️ Unauthorized access detected. Attempting token refresh...");

            var newToken = await _tokenRefreshService.TryRefreshTokenAsync();
            if (!string.IsNullOrEmpty(newToken))
            {
                _logger.LogInformation("🔁 Token successfully refreshed. Retrying request...");

                var newRequest = new HttpRequestMessage(request.Method, request.RequestUri!)
                {
                    Content = request.Content
                };

                foreach (var header in request.Headers)
                    if (header.Key != "Authorization")
                        newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

                newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                response = await _httpClient.SendAsync(newRequest);
            }
            else
            {
                _logger.LogWarning("❌ Token refresh failed. Request will not be retried.");
            }
        }

        return response;
    }

    #endregion
}



///// *****************************************************************************************
///// @file AuthorizedHttpClient.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-05-10
///// @brief Wraps HttpClient with automatic JWT handling and token refresh (with Serilog logging).
///// @details
///// Adds bearer token from secure storage to every request and refreshes the token if expired.
///// If the refreshed token still causes 401, a new HttpRequestMessage is built for a final retry.
///// Supports GET, POST, PUT, DELETE, and multipart/form-data operations.
///// *****************************************************************************************

//using Microsoft.Extensions.Logging;
//using System.Net;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;
//using UserFlow.API.Http.Auth;
//using UserFlow.API.HTTP.Services.Interfaces;

//namespace UserFlow.API.HTTP;

///// <summary>
///// 🌐 HTTP wrapper that automatically injects the JWT token and refreshes it if necessary.
///// </summary>
//public class AuthorizedHttpClient
//{
//    private readonly HttpClient _httpClient;
//    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
//    private readonly ITokenRefreshService _tokenRefreshService;
//    private readonly ISecureTokenStore _tokenStore;
//    private readonly ILogger<AuthorizedHttpClient> _logger;

//    /// <summary>
//    /// 🔧 Constructor injecting required services.
//    /// </summary>
//    public AuthorizedHttpClient(
//        HttpClient httpClient,
//        ITokenRefreshService tokenRefreshService,
//        ISecureTokenStore tokenStore,
//        ILogger<AuthorizedHttpClient> logger)
//    {
//        _httpClient = httpClient;
//        _tokenRefreshService = tokenRefreshService;
//        _tokenStore = tokenStore;
//        _logger = logger;
//    }

//    /// <summary>
//    /// 📄 Performs a GET request and parses the JSON response.
//    /// </summary>
//    public async Task<T?> GetAsync<T>(string url)
//    {
//        var request = new HttpRequestMessage(HttpMethod.Get, url);
//        var response = await SendAsync(request);
//        return await HandleResponse<T>(response);
//    }

//    /// <summary>
//    /// 📤 Performs a POST request and returns typed response.
//    /// </summary>
//    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
//    {
//        var json = JsonSerializer.Serialize(data);
//        var content = new StringContent(json, Encoding.UTF8, "application/json");
//        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

//        var response = await SendAsync(request);
//        return await HandleResponse<TResponse>(response);
//    }

//    /// <summary>
//    /// 📤 Performs a POST request and returns raw HttpResponseMessage.
//    /// </summary>
//    public async Task<HttpResponseMessage> PostAsync<TRequest>(string url, TRequest data)
//    {
//        var json = JsonSerializer.Serialize(data);
//        var content = new StringContent(json, Encoding.UTF8, "application/json");
//        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
//        return await SendAsync(request);
//    }

//    /// <summary>
//    /// 📥 Performs a PUT request with JSON payload.
//    /// </summary>
//    public async Task<HttpResponseMessage> PutAsync<TRequest>(string url, TRequest data)
//    {
//        var json = JsonSerializer.Serialize(data);
//        var content = new StringContent(json, Encoding.UTF8, "application/json");
//        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
//        return await SendAsync(request);
//    }

//    /// <summary>
//    /// ❌ Performs a DELETE request.
//    /// </summary>
//    public async Task<HttpResponseMessage> DeleteAsync(string url)
//    {
//        var request = new HttpRequestMessage(HttpMethod.Delete, url);
//        return await SendAsync(request);
//    }

//    /// <summary>
//    /// 🧪 Performs a raw GET request and returns unparsed response.
//    /// </summary>
//    public async Task<HttpResponseMessage> GetRawAsync(string url)
//    {
//        var token = await _tokenStore.GetAccessTokenAsync();
//        var request = new HttpRequestMessage(HttpMethod.Get, url);

//        if (!string.IsNullOrWhiteSpace(token))
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

//        return await _httpClient.SendAsync(request);
//    }

//    /// <summary>
//    /// 🧠 Handles the JSON deserialization of a response.
//    /// </summary>
//    private async Task<T?> HandleResponse<T>(HttpResponseMessage response)
//    {
//        if (!response.IsSuccessStatusCode)
//        {
//            _logger.LogWarning("❌ API responded with status code {StatusCode}", response.StatusCode);
//            return default;
//        }

//        var json = await response.Content.ReadAsStringAsync();

//        try
//        {
//            var res = JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);

//            if (res == null)
//            {
//                _logger.LogWarning("❌ Deserialized response is null. JSON: {Json}", json);
//            }

//            return res;
//        }
//        catch (JsonException ex)
//        {
//            _logger.LogError(ex, "❌ JSON deserialization error. Raw response: {Json}", json);
//            throw; // bewusst Exception hochwerfen, damit Fehler sofort sichtbar wird
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Unexpected error during JSON deserialization");
//            throw;
//        }
//    }
//    //private async Task<T?> HandleResponse<T>(HttpResponseMessage response)
//    //{
//    //    if (!response.IsSuccessStatusCode)
//    //        return default;

//    //    var json = await response.Content.ReadAsStringAsync();

//    //    T? res = default!;

//    //    try
//    //    {
//    //        res = JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        string err = ex.Message;
//    //    }

//    //    return res; 


//    //    //return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
//    //    //{
//    //    //    PropertyNameCaseInsensitive = true
//    //    //});
//    //}

//    /// <summary>
//    /// 🚦 Sends the request with JWT and retries with a fresh message if token expired.
//    /// </summary>
//    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
//    {
//        var token = await _tokenStore.GetAccessTokenAsync();
//        if (!string.IsNullOrEmpty(token))
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

//        var response = await _httpClient.SendAsync(request);

//        if (response.StatusCode == HttpStatusCode.Unauthorized)
//        {
//            _logger.LogWarning("⚠️ Unauthorized access detected. Attempting token refresh...");

//            var newToken = await _tokenRefreshService.TryRefreshTokenAsync();
//            if (!string.IsNullOrEmpty(newToken))
//            {
//                _logger.LogInformation("🔁 Token successfully refreshed. Retrying request...");

//                // 🆕 Neuer Request, da alter bereits gesendet wurde
//                var newRequest = new HttpRequestMessage(request.Method, request.RequestUri!)
//                {
//                    // ⚠️ Content kopieren, falls vorhanden
//                    Content = request.Content is StringContent sc
//                        ? new StringContent(await sc.ReadAsStringAsync(), Encoding.UTF8, sc.Headers.ContentType?.MediaType)
//                        : request.Content
//                };

//                // 📋 Alle Header übernehmen (außer Authorization)
//                foreach (var header in request.Headers)
//                {
//                    if (header.Key != "Authorization")
//                        newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
//                }

//                newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
//                response = await _httpClient.SendAsync(newRequest);
//            }
//            else
//            {
//                _logger.LogWarning("❌ Token refresh failed. Request will not be retried.");
//            }
//        }

//        return response;
//    }
//}

///// <remarks>
///// 🛠️ **Developer Notes**
///// - ✅ Avoids InvalidOperationException by creating a new request object for retry.
///// - 🔁 Automatic token refresh logic is now fully retry-safe.
///// - 🔐 Reads tokens from ISecureTokenStore and refreshes via ITokenRefreshService.
///// - 🧪 Works with POST/PUT as well – content is preserved on retry.
///// </remarks>
