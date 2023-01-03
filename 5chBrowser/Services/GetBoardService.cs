using _5chBrowser.Models;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CommunityToolkit.WinUI.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace _5chBrowser.Services
{
    public class GetBoardService
    {
        private ObservableCollection<BoardList> TreeSource = new ObservableCollection<BoardList>();
        public async Task<ObservableCollection<BoardList>> GetBoard()
        {
            string html = await Task.Run(() => GetHTML().Result);
            return HTMLParse(html);
        }
        private async Task<string> GetHTML()
        {
            HttpClient client = new HttpClient();
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                using HttpResponseMessage response = await client.GetAsync("https://itest.5ch.net/");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                return responseBody;
            }
            catch (HttpRequestException ex)
            {
                return ex.Message;
            }
        }
        private ObservableCollection<BoardList> HTMLParse(string html)
        {
            HtmlParser parser=new HtmlParser();
            IHtmlDocument doc =parser.ParseDocument(html);

            var Nodes = doc.GetElementById("bbsmenu");
            var categoryNodes = Nodes.GetElementsByClassName("pure-menu-list");
            foreach (var n in categoryNodes)
            {
                var categoryName = n.QuerySelector("strong").InnerHtml;
                var categoriesNode = n.GetElementsByClassName("pure-menu-link-board");

                var childrenList = new ObservableCollection<BoardList>();
                foreach (var c in categoriesNode)
                {
                    string boardName = c.InnerHtml;
                    string href = c.GetAttribute("href");
                    string link = $"https://itest.5ch.net{href}";
                    {
                        childrenList.Add(new BoardList()
                        {
                            BoardTitle = boardName,
                            BoardURL = link
                        });
                    }
                    
                }
                TreeSource.Add(new BoardList()
                {
                    BoardTitle = categoryName,
                    Children = childrenList
                });
            }
            return TreeSource;
        }
    }
}
