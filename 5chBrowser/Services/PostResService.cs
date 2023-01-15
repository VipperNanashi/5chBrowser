using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using _5chBrowser.Models;
using System.Web;
using Windows.Devices.AllJoyn;
using System.Net.Http.Headers;
using System.IO;

namespace _5chBrowser.Services
{
    public class PostResService
    {
        private HttpClient client;

        public PostResService()
        {
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy("http://localhost:8080", false),
                UseProxy = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            client = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<string> Post(string server, string bbs, string key, string name, string mail, string message, string oekakiBase64Data = "")
        {
            var request = ConstructJaneXenoHttpRequest(server, bbs, key, name, mail, message, oekakiBase64Data);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = "";
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream, Encoding.GetEncoding("Shift_JIS")))
                responseBody = await reader.ReadToEndAsync();

            return responseBody;
        }

        private HttpRequestMessage ConstructMSEdgeHttpsRequest(string server, string bbs, string key, string name, string mail, string message, string oekakiBase64Data)
        {
            var timestamp = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{server}/test/bbs.cgi");
            request.Version = Version.Parse("1.1");

            var parameters = new Dictionary<string, string>();
            parameters.Add("FROM", name);
            parameters.Add("mail", mail);
            parameters.Add("MESSAGE", message);
            parameters.Add("bbs", bbs);
            parameters.Add("key", key);
            parameters.Add("time", timestamp);
            parameters.Add("submit", "書き込む");
            parameters.Add("oekaki_thread1", oekakiBase64Data);

            var requestBodyText = string.Join("&", parameters.Select(p => p.Key + "=" + Encode(p.Value)));
            var content = new ByteArrayContent(Encoding.ASCII.GetBytes(requestBodyText));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            request.Content = content;

            request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
            request.Headers.TryAddWithoutValidation("Cache-Control", "max-age=0");
            request.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Not?A_Brand\";v=\"8\", \"Chromium\";v=\"108\", \"Microsoft Edge\";v=\"108\"");
            request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
            request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
            request.Headers.TryAddWithoutValidation("Origin", $"https://{server}");
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36 Edg/108.0.1462.76");
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
            request.Headers.TryAddWithoutValidation("Referer", $"https://{server}/test/read.cgi/{bbs}/{key}/");
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
            request.Headers.TryAddWithoutValidation("Accept-Language", "ja,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
            request.Headers.TryAddWithoutValidation("Cookie", "yuki=akari");

            return request;
        }

        private HttpRequestMessage ConstructJaneXenoHttpRequest(string server, string bbs, string key, string name, string mail, string message, string oekakiBase64Data = "")
        {
            var timestamp = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();

            var request = new HttpRequestMessage(HttpMethod.Post, $"http://{server}/test/bbs.cgi");
            request.Version = Version.Parse("1.0");

            var parameters = new Dictionary<string, string>();
            parameters.Add("submit", "書き込む");
            parameters.Add("FROM", name);
            parameters.Add("mail", mail);
            parameters.Add("MESSAGE", message);
            parameters.Add("bbs", bbs);
            parameters.Add("key", key);
            parameters.Add("time", timestamp);

            // Xenoでは存在しない
            if (oekakiBase64Data != "")
                parameters.Add("oekaki_thread1", oekakiBase64Data);

            var requestBodyText = string.Join("&", parameters.Select(p => p.Key + "=" + Encode(p.Value)));
            var content = new ByteArrayContent(Encoding.ASCII.GetBytes(requestBodyText));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            request.Content = content;

            request.Headers.TryAddWithoutValidation("Connection", "close");
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.TryAddWithoutValidation("Referer", $"http://{server}/{bbs}/");
            request.Headers.TryAddWithoutValidation("User-Agent", "Monazilla/1.00 (JaneXeno/220330)");
            request.Headers.TryAddWithoutValidation("Cookie", "yuki=akari");

            return request;
        }

        private string Encode(string input)
        {
            var enc = Encoding.GetEncoding("Shift_JIS");
            var tmp = HttpUtility.UrlEncode(input, enc);
            return Regex.Replace(tmp, @"%[0-9a-f]{2}", s => s.Value.ToUpper());
        }
    }
}
