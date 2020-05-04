using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GoogleOAuth.Editor
{
    /// <summary>
    /// The class to authenticate with OAuth2 and get an authorization code.
    /// </summary>
    public class AuthorizationCodeProvider : IDisposable
    {
        public class Result
        {
            public string AuthorizationCode { get; internal set; }
            public string CodeVerifier { get; internal set; }
            public string RedirectUri { get; internal set; }
        }

        /// <summary>
        /// The message that displayed on the authorization completion page.
        /// You can use HTML tags.
        /// </summary>
        public string CompletionMessage { get; set; } = "Authorization succeeded.<br>Go back to the Unity Editor and continue.";

        private readonly string _clientId;
        private ProvideHandle<Result> _handle;
        private HttpListener _httpListener;

        public AuthorizationCodeProvider(string clientId) => _clientId = clientId;

        /// <summary>
        /// Display OAuth2 authorization page and provide authorization code.
        /// </summary>
        public async Task<ProvideHandle<Result>> ProvideAsync()
        {
            Clear();

            // https://developers.google.com/identity/protocols/oauth2/web-server
            var clientId = _clientId;
            var authUriStr = "https://accounts.google.com/o/oauth2/v2/auth";
            // scope : https://developers.google.com/identity/protocols/oauth2/scopes
            var scopes = "https://www.googleapis.com/auth/spreadsheets.readonly";
            var codeVerifier = GetRandomStringForUrl(32);
            var codeChallenge = ConvertToBase64Url(Sha256(codeVerifier));
            var state = GetRandomStringForUrl(32);
            var redirectPort = GetRandomPort();
            var redirectUri = $"http://localhost:{redirectPort}";
            var redirectUriWithSlash = $"{redirectUri}/";

            // Create a uri for the authorization and open it in a web browser.
            var uriBuilder = new UriBuilder(authUriStr);
            AppendParameter(uriBuilder, "client_id", clientId);
            AppendParameter(uriBuilder, "redirect_uri", redirectUri);
            AppendParameter(uriBuilder, "response_type", "code");
            AppendParameter(uriBuilder, "scope", scopes);
            AppendParameter(uriBuilder, "code_challenge", codeChallenge);
            AppendParameter(uriBuilder, "code_challenge_method", "S256");
            AppendParameter(uriBuilder, "state", state);
            var uri = uriBuilder.Uri;
            Application.OpenURL(uri.ToString());

            // Wait redirect from the authorization completion page.
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(redirectUriWithSlash);
            httpListener.Start();
            var taskCompletionSource = new TaskCompletionSource<IAsyncResult>();
            httpListener.BeginGetContext(x => taskCompletionSource.SetResult(x), httpListener);
            _httpListener = httpListener;

            var asyncResult = await taskCompletionSource.Task;

            var result = new Result();
            result.CodeVerifier = codeVerifier;
            result.RedirectUri = redirectUri;

            _handle = new ProvideHandle<Result>();
            
            var context = httpListener.EndGetContext(asyncResult);
            var request = context.Request;
            var response = context.Response;

            // Display the authorization completion message
            var message = Encoding.UTF8.GetBytes($"<html><head><meta charset='utf-8'/></head><body>{CompletionMessage}</body></html>");
            response.OutputStream.Write(message, 0, message.Length);

            // Close listener
            response.OutputStream.Close();
            httpListener.Close();

            var responseError = request.QueryString.Get("error");
            var responseCode = request.QueryString.Get("code");
            var responseState = request.QueryString.Get("state");

            // Check errors
            if (responseError != null)
            {
                _handle?.Fail(new NetworkException($"OAuth error: {responseError}", response.StatusCode));
                return _handle;
            }

            if (request.QueryString.Get("code") == null || request.QueryString.Get("state") == null)
            {
                _handle?.Fail(new NetworkException($"OAuth error: invalid response.", response.StatusCode));
                return _handle;
            }

            if (responseState != state)
            {
                _handle?.Fail(new NetworkException($"OAuth error: Request has invalid state.", response.StatusCode));
                return _handle;
            }

            result.AuthorizationCode = responseCode;
            _handle?.Success(result);

            return _handle;
        }

        public void Dispose()
        {
            Clear();
        }

        private void Clear()
        {
            _handle?.Fail(new Exception("The operation has been canceled."));
            _httpListener?.Close();
            _handle = null;
        }

        private static int GetRandomPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static void AppendParameter(UriBuilder uriBuilder, string name, string value)
        {
            var query = uriBuilder.Query;
            var delimiter = string.IsNullOrEmpty(query) ? "?" : "&";
            query += $"{delimiter}{name}={value}";
            uriBuilder.Query = query;
        }

        private static string GetRandomStringForUrl(uint length)
        {
            var cryptoServiceProvider = new RNGCryptoServiceProvider();
            var bytes = new byte[length];
            cryptoServiceProvider.GetBytes(bytes);
            return ConvertToBase64Url(bytes);
        }

        private static string ConvertToBase64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private static byte[] Sha256(string source)
        {
            var bytes = Encoding.ASCII.GetBytes(source);
            return new SHA256Managed().ComputeHash(bytes);
        }
    }
}