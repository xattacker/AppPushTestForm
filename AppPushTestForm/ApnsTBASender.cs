using HttpTwo;
using Jose;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Net;
using PushSharp.Sender;

namespace AppPush
{
    // TokenBasedAuthentication
    class ApnsTBASender
    {
        public async void Push
        (
        ApnsAuthParas paras,
        PushMessage message, 
        List<string> deviceTokens,
        Action<bool, string> callback,
        Action<Exception> onExcepted)
        {
            // load p8 key content
            var privateKeyContent = System.IO.File.ReadAllText(paras.KeyPath);
            var privateKeyList = privateKeyContent.Split('\n').ToList();
            var privateKey = privateKeyList.Where((s, i) => i != 0 && i != privateKeyList.Count - 1)
                                               .Aggregate((agg, s) => agg + s);

            var secretKeyFile = Convert.FromBase64String(privateKey);
            var secretKey = CngKey.Import(secretKeyFile, CngKeyBlobFormat.Pkcs8PrivateBlob);

            var expiration = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var expirationSeconds = (long)expiration.TotalSeconds;

            var payload = new Dictionary<string, object>()
            {
                { "iss", paras.TeamID },
                { "iat", expirationSeconds }
            };
            var header = new Dictionary<string, object>()
            {
                { "alg", "ES256" },
                { "kid", paras.KeyID }
            };

            string accessToken = Jose.JWT.Encode(payload, secretKey, JwsAlgorithm.ES256, header);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                                         SecurityProtocolType.Tls11 |
                                         SecurityProtocolType.Tls12 |
                                         SecurityProtocolType.Ssl3;

            //Development server: api.development.push.apple.com:443
            //Production server: api.push.apple.com:443
            string host = paras.ServerMode ? "api.push.apple.com" : "api.development.push.apple.com";
            int port = 443;

            // Uri to request, only send one device token now
            var uri = new Uri(string.Format("https://{0}:{1}/3/device/{2}", host, port, deviceTokens[0]));

            var payloadData = JObject.FromObject(new
            {
                aps = new
                {
                    alert = message.Aps.Alert,
                    badge = message.Aps.Badge,
                    sound = message.Aps.Sound
                }
            });

            byte[] data = System.Text.Encoding.UTF8.GetBytes(payloadData.ToString());

            var handler = new Http2MessageHandler();
            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestMessage = new HttpRequestMessage();
            requestMessage.RequestUri = uri;
            //requestMessage.Headers.Add("authorization", string.Format("bearer {0}", accessToken));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
            requestMessage.Headers.Add("apns-id", Guid.NewGuid().ToString());
            requestMessage.Headers.Add("apns-expiration", "0");
            requestMessage.Headers.Add("apns-priority", "10");
            requestMessage.Headers.Add("apns-topic", paras.BundleID);
            requestMessage.Method = HttpMethod.Post;
            requestMessage.Content = new ByteArrayContent(data);
            requestMessage.Version = new Version(2, 0);

            try
            {
                var responseMessage = await httpClient.SendAsync(requestMessage);

                bool succeed = false;
                string response = null;

                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    succeed = true;

                    string responseUuid = string.Empty;
                    IEnumerable<string> values;
                    if (responseMessage.Headers.TryGetValues("apns-id", out values))
                    {
                        responseUuid = values.First();
                    }

                    response = responseUuid;
                }
                else
                {
                    var body = await responseMessage.Content.ReadAsStringAsync();
                    var json = new JObject();
                    json = JObject.Parse(body);

                    response = json.Value<string>("reason");
                }

                callback(succeed, response);

            }
            catch (Exception ex)
            {
                onExcepted(ex);
            }
        }
    }
}
