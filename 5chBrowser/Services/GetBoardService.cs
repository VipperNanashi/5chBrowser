using _5chBrowser.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace _5chBrowser.Services
{
    public class GetBoardService
    {
        private ObservableCollection<BoardList> TreeSource = new ObservableCollection<BoardList>();
        public async Task<ObservableCollection<BoardList>> GetBoard()
        {
            var json = await Task.Run(() => GetJson().Result);
            BoardInfo boardInfo = JsonAnalize(json);
            return HTMLParse(boardInfo);
        }
        private async Task<string> GetJson()
        {
            HttpClient client = new HttpClient();
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                using HttpResponseMessage response = await client.GetAsync("https://menu.5ch.net/bbsmenu.json");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;                
            }
            catch(Exception ex) 
            {
                return ex.Message;
            }
        }
        private BoardInfo JsonAnalize(string json)
        {
            var col = JsonSerializer.Deserialize<BoardInfo>(json);
            return col;
        }
        private ObservableCollection<BoardList> HTMLParse(BoardInfo boardInfo)
        {

            foreach (var m in boardInfo.menu_list)
            {

                var childrenList = new ObservableCollection<BoardList>();
                foreach (var c in m.category_content)
                {
                    childrenList.Add(new BoardList()
                    {
                        BoardTitle = c.board_name,
                        BoardURL = c.url
                    });
                }
                TreeSource.Add(new BoardList()
                {
                    BoardTitle = m.category_name,
                    Children = childrenList
                });
            }
            return TreeSource;
        }
        
    }
}
