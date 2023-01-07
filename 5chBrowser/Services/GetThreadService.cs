using _5chBrowser.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace _5chBrowser.Services
{
    public class GetThreadService
    {
        private ObservableCollection<BoardList> ThreadSource = new ObservableCollection<BoardList>();
        public async Task<ObservableCollection<ThreadList>> GetThread(string url)
        {
            string txt = await Task.Run(() => GetText(url).Result);
        }
        private async Task<string> GetText(string url)
        {
            HttpClient client = new HttpClient();
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            string text = url + "lastmodify.txt";
            try
            {
                using HttpResponseMessage response = await client.GetAsync(text);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
