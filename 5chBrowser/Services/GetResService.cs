using _5chBrowser.Models;
using _5chBrowser.Services;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CommunityToolkit.WinUI.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace _5chBrowser.Services
{
    public class GetResService
    {
        public enum GetMode { LocalRemote, Local, Remote }

        private HttpClient client;

        private static Regex resRegex = new Regex(@"^(.*?)<>(.*?)<>(.*?)<> (.*?) <>((.+) )?$");
        private static Dictionary<string, object> lockList = new();

        public GetResService()
        {
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy("http://localhost:8080", false),
                UseProxy = true
            };

            client = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        // mode:LocalRemote 差分のみ取得してローカルのDATに足し合わせたものを返却(デフォルト)
        // mode:Local       ローカルのDATを返却
        // mode:Remote      全体を再取得して返却
        public async Task<ObservableCollection<Res>> GetRes(string server, string bbs, string key, GetMode mode = GetMode.LocalRemote)
        {
            var (_, resList) = await GetTitleAndRes(server, bbs, key, mode);
            return resList;
        }

        // DATからスレタイとレス一覧を取得
        public async Task<(string, ObservableCollection<Res>)> GetTitleAndRes(string server, string bbs, string key, GetMode mode = GetMode.LocalRemote)
        {
            // 同じスレを同時に取得しない
            var id = server + "_" + bbs + "_" + key;
            var dat = await RunExclusive(() => GetDat(server, bbs, key, mode), id);
            var (title, resList) = TitleAndResListFromDat(dat);
            return (title, new ObservableCollection<Res>(resList));
        }

        private async Task<T> RunExclusive<T>(Func<Task<T>> func, string id)
        {
            return await Task.Run(() =>
            {
                object lockObj;
                lock (lockList)
                {
                    lockObj = lockList.FirstOrDefault(item => item.Key == id).Value ?? (lockList[id] = new());

                    lockList.Remove(id);
                    lockList.Add(id, lockObj);

                    if (lockList.Count > 200)
                        lockList.Remove(lockList.First().Key);
                }

                T result;
                lock (lockObj)
                    result = func().Result;

                return result;
            });
        }

        private async Task<string> GetDat(string server, string bbs, string key, GetMode mode)
        {
            switch (mode)
            {
                case GetMode.LocalRemote:
                    await DownloadDat(server, bbs, key, false);
                    break;
                case GetMode.Local:
                    break;
                case GetMode.Remote:
                    await DownloadDat(server, bbs, key, true);
                    break;
                default:
                    throw new NotImplementedException("指定された取得モードは非対応です。");
            }

            return await LoadDat(server, bbs, key);
        }

        // server,bbsからフォルダーを特定して返却
        private async Task<string> GetFolder(string server, string bbs)
        {
            // 未実装
            // return @$"{logFolder}\Logs\{site}\{category}\{boardName}"
            return @"C:\log\Logs\2ch\ＰＣ等\ソフトウェア";
        }

        private async Task DownloadDat(string server, string bbs, string key, bool reload)
        {
            string lastModified = "";
            long? range = null;

            if (!reload)
                (lastModified, range) = await GetInfo(server, bbs, key);

            var req = new HttpRequestMessage(HttpMethod.Get, $"http://{server}/{bbs}/dat/{key}.dat");
            if (lastModified != "")
                req.Headers.Add("if-modified-since", lastModified);
            if (range != null)
                req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(range, null);

            var response = await client.SendAsync(req);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotModified)
                    return;
            }

            var newLastModified = response.Headers.FirstOrDefault(pair => pair.Key.ToLower() == "Last-Modified".ToLower()).Value?.FirstOrDefault() ?? "";

            var responseBody = "";
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream, Encoding.GetEncoding("Shift_JIS")))
                responseBody = await reader.ReadToEndAsync();

            await SaveDat(server, bbs, key, responseBody, !reload);
            await SaveInfo(server, bbs, key, lastModified);
        }

        // 指定されたスレッドのLastModifiedとRangeを取得
        private async Task<(string, long?)> GetInfo(string server, string bbs, string key)
        {
            var folder = await GetFolder(server, bbs);

            string lastModified;
            var idxPath = folder + "\\" + key + ".idx";
            if (File.Exists(idxPath))
            {
                var text = await File.ReadAllLinesAsync(idxPath);
                lastModified = text[1];
            }
            else
            {
                lastModified = "";
            }

            long? range;
            var datPath = folder + "\\" + key + ".dat";
            if (File.Exists(datPath))
            {
                // ファイルのサイズを取得
                var fi = new FileInfo(datPath);
                // rangeをファイルサイズと同じにすると差分なしのときにHTTPエラーが返ってくる
                // プロキシソフトによってエラーが異なるので1バイト後ろから取得して後で削除する
                range = fi.Length - 1;
            }
            else
            {
                range = null;
            }

            return (lastModified, range);
        }

        private async Task SaveDat(string server, string bbs, string key, string dat, bool append)
        {
            var path = await GetFolder(server, bbs) + "\\" + key + ".dat";
            var enc = Encoding.GetEncoding("Shift_JIS");

            if (append)
                await File.AppendAllTextAsync(path, dat.TrimStart('\n'), enc);
            else
                await File.WriteAllTextAsync(path, dat, enc);
        }

        private async Task SaveInfo(string server, string bbs, string key, string lastModified)
        {
            var path = await GetFolder(server, bbs) + "\\" + key + ".idx";
            string[] lines;
            if (File.Exists(path))
                lines = await File.ReadAllLinesAsync(path);
            else
                lines = new string[15];
            lines[1] = lastModified;
            await File.WriteAllLinesAsync(path, lines);
        }

        private async Task<string> LoadDat(string server, string bbs, string key)
        {
            var path = await GetFolder(server, bbs) + "\\" + key + ".dat";
            var enc = Encoding.GetEncoding("Shift_JIS");
            return await File.ReadAllTextAsync(path, enc);
        }

        private (string, List<Res>) TitleAndResListFromDat(string dat)
        {
            var lines = dat.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var title = "";
            var resList = new List<Res>();

            for (int i = 0; i < lines.Length; i++)
            {
                var match = resRegex.Match(lines[i]);

                var no = i + 1;
                var name = match.Groups[1].Value;
                var mail = match.Groups[2].Value;
                var options = match.Groups[3].Value;
                var message = match.Groups[4].Value;

                if (match.Groups[6].Value != "")
                    title = match.Groups[6].Value;

                resList.Add(new Res(no, name, mail, options, message));
            }

            return (title, resList);
        }
    }
}
