using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GoogleOAuth.Editor
{
    public class AccessTokenProvider
    {
        [Serializable]
        public class Result
        {
            [SerializeField] private string access_token = default;
            public string AccessToken => access_token;
            [SerializeField] private string expires_in = default;
            public string ExpiresIn => expires_in;
            [SerializeField] private string id_token = default;
            public string IdToken => id_token;
            [SerializeField] private string refresh_token = default;
            public string RefreshToken => refresh_token;
            [SerializeField] private string scope = default;
            public string Scope => scope;
            [SerializeField] private string token_type = default;
            public string TokenType => token_type;
        }

        private readonly string _clientId;
        private readonly string _clientSecret;

        public AccessTokenProvider(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public async Task<ProvideHandle<Result>> ProvideAsync(string authorizationCode, string codeVerifier, string redirectUri)
        {
            var form = new WWWForm();
            form.AddField("client_id", _clientId);
            form.AddField("client_secret", _clientSecret);
            form.AddField("code", authorizationCode);
            form.AddField("code_verifier", codeVerifier);
            form.AddField("grant_type", "authorization_code");
            form.AddField("redirect_uri", redirectUri);

            var taskCompletionSource = new TaskCompletionSource<AsyncOperation>();
            var request = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form);
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            request.SendWebRequest().completed += x => taskCompletionSource.SetResult(x);
            
            await taskCompletionSource.Task;
            var handle = new ProvideHandle<Result>();
            if (request.isHttpError || request.isNetworkError)
            {
                handle.Fail(new NetworkException(request.error, request.responseCode));
                return handle;
            }

            var response = JsonUtility.FromJson<Result>(request.downloadHandler.text);
            handle.Success(response);

            return handle;
        }
    }
}