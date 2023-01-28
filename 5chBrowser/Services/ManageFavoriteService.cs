using _5chBrowser.Models;
using ABI.Windows.AI.MachineLearning;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace _5chBrowser.Services
{
    public class ManageFavoritesService
    {
        private string GetPath()
        {
            var assembly = Assembly.GetEntryAssembly();
            var folder = Path.GetDirectoryName(assembly.Location);
            var path = Path.Combine(folder, "favorites.json");
            return path;
        }

        public async Task<ObservableCollection<FavoriteItem>> GetFavorites()
        {
            return await ExclusiveRunner.Run(async () =>
            {
                var orgList = new List<FavoriteItem>();
                var path = GetPath();
                try
                {
                    var orgJson = await File.ReadAllTextAsync(path);
                    orgList = JsonSerializer.Deserialize<List<FavoriteItem>>(orgJson, new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    });
                }
                catch { }
                return new ObservableCollection<FavoriteItem>(orgList);
            }, "favorites");
        }

        public async Task Add(BoardList board)
        {
            var item = new FavoriteItem(board.BoardTitle, board.Category.BoardTitle, board.BoardTitle, "");
            await Add(item);
        }

        public async Task Add(ThreadList thread)
        {
            var item = new FavoriteItem(thread.ThreadName, thread.Board.Category.BoardTitle, thread.Board.BoardTitle, thread.Dat);
            await Add(item);
        }

        private async Task Add(FavoriteItem item)
        {
            await ExclusiveRunner.Run(async () =>
             {
                 var list = await GetFavorites();
                 if (list.All(otherItem => JsonSerializer.Serialize(otherItem) != JsonSerializer.Serialize(item)))
                 {
                     list.Add(item);
                     var json = ToJson(list);
                     var path = GetPath();
                     await File.WriteAllTextAsync(path, json);
                 }
             }, "favorites");
        }

        private string ToJson(dynamic obj)
        {
            var jsonstr = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            });

            jsonstr = Regex.Replace(jsonstr, @"([^\\])\\u3000", "$1　");
            jsonstr = Regex.Replace(jsonstr, @"^\\u3000", "　");
            jsonstr = Regex.Replace(jsonstr, @"([^\\])\\u0022", "$1\\\"");
            jsonstr = Regex.Replace(jsonstr, @"^\\u0022", "\\\"");
            return jsonstr;
        }
    }
}
