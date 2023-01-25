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
using System.Xml.Linq;
using System.Net.Sockets;

namespace _5chBrowser.Services
{
    public class BuildThreadService
    {
        private HttpClient client;

        public BuildThreadService()
        {
            var socketHandler = new SocketsHttpHandler()
            {
                Proxy = new WebProxy(Properties.Settings.Default.ReadProxy, false),
                UseProxy = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                ConnectCallback = async (context, cancellationToken) =>
                {
                    // IPv6を使用しないようにする処理

                    // GetHostEntryAsyncでホスト名を解決する
                    // これに127.0.0.1を通すと192.168.10.10のように変換さえるが2chAPIProxyなどで接続拒否される
                    // ユーザーが意図しない変換は避けるためにIPアドレスの場合はそのまま使用
                    IPAddress address;
                    if (!IPAddress.TryParse(context.DnsEndPoint.Host, out address))
                    {
                        var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, AddressFamily.InterNetwork, cancellationToken);
                        address = entry.AddressList.FirstOrDefault();
                    }

                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true
                    };

                    try
                    {
                        await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };

            client = new HttpClient(socketHandler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<string> Build(string server, string bbs, string title, string name, string mail, string message)
        {
            var request = await ConstructJaneXenoHttpRequest(server, bbs, title, name, mail, message);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = "";
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream, Encoding.GetEncoding("Shift_JIS")))
                responseBody = await reader.ReadToEndAsync();

            return responseBody;
        }

        private async Task<HttpRequestMessage> ConstructMSEdgeHttpsRequest(string server, string bbs, string title, string name, string mail, string message)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{server}/test/bbs.cgi?guid=ON");
            request.Version = Version.Parse("1.1");

            var (timestamp, cert) = await GetCertInfo(server, bbs);

            var parameters = new Dictionary<string, string>();
            parameters.Add("submit", "新規スレッド作成");
            parameters.Add("subject", title);
            parameters.Add("FROM", name);
            parameters.Add("mail", mail);
            parameters.Add("MESSAGE", message);
            parameters.Add("site", "top_" + bbs);
            parameters.Add("bbs", bbs);
            parameters.Add("time", timestamp);
            parameters.Add("cert", cert);

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
            request.Headers.TryAddWithoutValidation("Referer", $"https://{server}/{bbs}/");
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            request.Headers.TryAddWithoutValidation("Accept-Language", "ja,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
            request.Headers.TryAddWithoutValidation("Cookie", "yuki=akari");

            return request;
        }

        private async Task<(string, string)> GetCertInfo(string server, string bbs)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{server}/{bbs}/");
            request.Version = Version.Parse("1.1");

            request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
            request.Headers.TryAddWithoutValidation("Cache-Control", "max-age=0");
            request.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Not?A_Brand\";v=\"8\", \"Chromium\";v=\"108\", \"Microsoft Edge\";v=\"108\"");
            request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
            request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36 Edg/108.0.1462.76");
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-site");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
            request.Headers.TryAddWithoutValidation("Referer", "https://menu.5ch.net/");
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            request.Headers.TryAddWithoutValidation("Accept-Language", "ja,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
            request.Headers.TryAddWithoutValidation("Cookie", "yuki=akari");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = "";
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream, Encoding.GetEncoding("Shift_JIS")))
                responseBody = await reader.ReadToEndAsync();

            var timestamp = Regex.Match(responseBody, @"<input.+?type=""submit"".+?value=""新規スレッド作成"".+?<input.+?name=""time"".+?value=""([0-9]+)", RegexOptions.Singleline).Groups[1].Value;
            var cert = Regex.Match(responseBody, @"<input.+?type=""submit"".+?value=""新規スレッド作成"".+?<input.+?name=""cert"".+?value=""([0-9a-z]+)", RegexOptions.Singleline).Groups[1].Value;
            return (timestamp, cert);
        }

        private async Task<HttpRequestMessage> ConstructJaneXenoHttpRequest(string server, string bbs, string title, string name, string mail, string message)
        {
            var timestamp = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();

            var request = new HttpRequestMessage(HttpMethod.Post, $"http://{server}/test/bbs.cgi");
            request.Version = Version.Parse("1.0");

            var parameters = new Dictionary<string, string>();
            parameters.Add("subject", title);
            parameters.Add("submit", "新規スレッド作成");
            parameters.Add("FROM", name);
            parameters.Add("mail", mail);
            parameters.Add("MESSAGE", message);
            parameters.Add("bbs", bbs);
            parameters.Add("time", timestamp);

            var requestBodyText = string.Join("&", parameters.Select(p => p.Key + "=" + Encode(p.Value)));
            var content = new ByteArrayContent(Encoding.ASCII.GetBytes(requestBodyText));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            request.Content = content;

            request.Headers.TryAddWithoutValidation("Connection", "close");
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.TryAddWithoutValidation("Referer", $"http://{server}/{bbs}/");
            request.Headers.TryAddWithoutValidation("User-Agent", "Monazilla/1.00 (JaneXeno/220330)");
            request.Headers.TryAddWithoutValidation("Cookie", "yuki=akari");

            return await Task.FromResult(request);
        }

        private string Encode(string input)
        {
            var enc = Encoding.GetEncoding("Shift_JIS");
            var tmp = HttpUtility.UrlEncode(input, enc);
            return Regex.Replace(tmp, @"%[0-9a-f]{2}", s => s.Value.ToUpper());
        }
    }
}
