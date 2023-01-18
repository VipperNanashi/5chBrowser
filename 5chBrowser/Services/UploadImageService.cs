using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http.Headers;
using _5chBrowser.Models;
using System.Text.Json;

namespace _5chBrowser.Services
{
    public class UploadImageService
    {
        private HttpClient client;

        public UploadImageService()
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

        public async Task<string> Upload(string filePath)
        {

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.imgur.com/3/image");
            request.Headers.TryAddWithoutValidation("Authorization", "Client-ID f8d7144b566519f");

            HttpResponseMessage response;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var content = new StreamContent(fs);
                content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
                request.Content = content;
                response = await client.SendAsync(request);
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            var url = data.GetProperty("data").GetProperty("link").GetString();
            var deleteHash = data.GetProperty("data").GetProperty("deletehash").GetString();
            return url;
        }
    }
}
