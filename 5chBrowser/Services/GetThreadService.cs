using _5chBrowser.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _5chBrowser.Services
{
    public class GetThreadService
    {
        private ObservableCollection<ThreadList> ThreadSource = new ObservableCollection<ThreadList>();
        public async Task<ObservableCollection<ThreadList>> GetThread(BoardList board)
        {
            string txt = await Task.Run(() => GetText(board).Result);

            return ThreadSourcePase(board, txt);
        }
        private async Task<string> GetText(BoardList board)
        {
            HttpClient client = new HttpClient();
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            string text = board.BoardURL + "lastmodify.txt";

            using HttpResponseMessage response = await client.GetAsync(text);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            string responseBody = "";
            using (var stream = (await response.Content.ReadAsStreamAsync()))
            using (var reader = (new StreamReader(stream, Encoding.GetEncoding("Shift_JIS")) as TextReader))
                responseBody = await reader.ReadToEndAsync();
            return responseBody;
        }
        private ObservableCollection<ThreadList> ThreadSourcePase(BoardList board, string txt)
        {
            string[] threadlist = txt.Split("\n");
            foreach (var list in threadlist)
            {
                string[] thread = list.Split("<>");
                if (thread.Length > 1)
                {
                    ThreadSource.Add(new ThreadList()
                    {
                        Board = board,
                        Dat = thread[0].Replace(".dat", ""),
                        ThreadName = thread[1],
                        ThreadCount = thread[2],
                        LastTime = thread[6],
                    });
                }
            }
            return ThreadSource;
        }
    }
}
