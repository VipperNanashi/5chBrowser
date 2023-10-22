using _5chBrowser.Models;
using _5chBrowser.Services;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CommunityToolkit.WinUI.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
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

        private static Regex resRegex = new Regex(@"^(.*?)<>(.*?)<>(.*?)<> (.*?) <>(.+)?$");

        public GetResService()
        {
            var socketHandler = new SocketsHttpHandler()
            {
                Proxy = new WebProxy(Properties.Settings.Default.ReadProxy, false),
                UseProxy = false,
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
                Timeout = TimeSpan.FromSeconds(10),
            };
        }

        // mode:LocalRemote 差分のみ取得してローカルのDATに足し合わせたものを返却(デフォルト)
        // mode:Local       ローカルのDATを返却
        // mode:Remote      全体を再取得して返却
        public async Task<ObservableCollection<Res>> GetRes(ThreadList thread, GetMode mode = GetMode.LocalRemote)
        {
            var (_, resList) = await GetTitleAndRes(thread, mode);
            return resList;
        }

        // DATからスレタイとレス一覧を取得
        public async Task<(string, ObservableCollection<Res>)> GetTitleAndRes(ThreadList thread, GetMode mode = GetMode.LocalRemote)
        {
            // 同じスレを同時に取得しない
            var id = thread.Board.BoardURL + "_" + thread.Dat;
            var dat = await ExclusiveRunner.Run(() => GetDat(thread, mode), id);
            var (title, resList) = TitleAndResListFromDat(dat);
            return (title, new ObservableCollection<Res>(resList));
        }

        private async Task<string> GetDat(ThreadList thread, GetMode mode)
        {
            switch (mode)
            {
                case GetMode.LocalRemote:
                    await DownloadDat(thread, false);
                    break;
                case GetMode.Local:
                    break;
                case GetMode.Remote:
                    await DownloadDat(thread, true);
                    break;
                default:
                    throw new NotImplementedException("指定された取得モードは非対応です。");
            }

            return await LoadDat(thread);
        }

        // server,bbsからフォルダーを特定して返却（なければ作成）
        private string GetFolder(BoardList board)
        {
            var logFolderPath = Properties.Settings.Default.LogFolder;
            if (logFolderPath == "")
            {
                var assembly = Assembly.GetEntryAssembly();
                logFolderPath = Path.GetDirectoryName(assembly.Location);
            }

            var rootFolder = "Log";
            var siteName = board.SiteName;
            var categoryName = board.Category.BoardTitle;
            var boardName = board.BoardTitle;

            var datFolderPath = Path.Combine(logFolderPath, rootFolder, siteName, categoryName, boardName);

            if (!Directory.Exists(datFolderPath))
                Directory.CreateDirectory(datFolderPath);

            return datFolderPath;
        }

        private async Task DownloadDat(ThreadList thread, bool reload)
        {
            string lastModified = "";
            long? range = null;

            if (!reload)
                (lastModified, range) = await GetInfo(thread);

            var datUrl = Regex.Replace(thread.Board.BoardURL, @"^https://", "http://") + "dat/" + thread.Dat + ".dat";
            var request = new HttpRequestMessage(HttpMethod.Get, datUrl);
            if (lastModified != "")
                request.Headers.Add("if-modified-since", lastModified);
            if (range != null)
                request.Headers.Range = new RangeHeaderValue(range, null);

            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");

            var response = await client.SendAsync(request);

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

            await SaveDat(thread, responseBody, !reload);
            await SaveInfo(thread, newLastModified);
        }

        // 指定されたスレッドのLastModifiedとRangeを取得
        private async Task<(string, long?)> GetInfo(ThreadList thread)
        {
            var folder = GetFolder(thread.Board);

            string lastModified;
            var idxPath = Path.Combine(folder, thread.Dat + ".idx");
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
            var datPath = Path.Combine(folder, thread.Dat + ".dat");
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

        private async Task SaveDat(ThreadList thread, string dat, bool append)
        {
            var path = Path.Combine(GetFolder(thread.Board), thread.Dat + ".dat");
            var enc = Encoding.GetEncoding("Shift_JIS");

            if (append)
                await File.AppendAllTextAsync(path, dat.TrimStart('\n'), enc);
            else
                await File.WriteAllTextAsync(path, dat, enc);
        }

        private async Task SaveInfo(ThreadList thread, string lastModified)
        {
            var path = Path.Combine(GetFolder(thread.Board), thread.Dat + ".idx");
            string[] lines;
            if (File.Exists(path))
                lines = await File.ReadAllLinesAsync(path);
            else
                lines = new string[15];
            lines[1] = lastModified;
            await File.WriteAllLinesAsync(path, lines);
        }

        private async Task<string> LoadDat(ThreadList thread)
        {
            var path = Path.Combine(GetFolder(thread.Board), thread.Dat + ".dat");
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

                if (match.Groups[5].Value != "")
                    title = match.Groups[5].Value.Trim();

                resList.Add(new Res(no, name, mail, options, message));
            }

            return (title, resList);
        }
    }
}
