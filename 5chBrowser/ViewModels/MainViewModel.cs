using _5chBrowser.Services;
using _5chBrowser.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using WinRT;
using System.Text.RegularExpressions;

namespace _5chBrowser.ViewModels
{
    public partial class MainViewModel:ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<BoardList> boardSource=new ObservableCollection<BoardList>();
        [ObservableProperty]
        public ObservableCollection<ThreadList> threadSource=new ObservableCollection<ThreadList>();
        [ObservableProperty]
        public ObservableCollection<Res> resSource=new ObservableCollection<Res>();

        //GetRes用
        [ObservableProperty]
        public string selectServer;
        [ObservableProperty]
        public string selectBBS;
        [ObservableProperty]
        public string selectKey;
        
        public MainViewModel()
        {
            GetBoardList();
        }
        private async void GetBoardList()
        {
            GetBoardService getBoardService= new GetBoardService();
            BoardSource = await getBoardService.GetBoard();
        }
        [RelayCommand]
        public async void SelectBoard(TreeViewItemInvokedEventArgs args)
        {
            var node = args.InvokedItem as BoardList;
            if(node.BoardURL == null) {
                return; 
            }
            var threadURL=node.BoardURL;

            //server,bbs取得
            string delimited = @"/([.a-z1-9\\s]*)";
            var match = Regex.Matches(threadURL, delimited);
            SelectServer = match[1].Groups[1].Value;
            SelectBBS = match[2].Groups[1].Value;
            GetThreadService getThreadService= new GetThreadService();
            ThreadSource = await getThreadService.GetThread(threadURL.ToString());
        }
        [RelayCommand]
        public async void SelectThread(SelectionChangedEventArgs args)
        {
            var selectItem = args.AddedItems.Cast<ThreadList>().FirstOrDefault();
            if(selectItem == null)
            { return; }
            SelectKey = selectItem.Dat;
            GetResService getResService= new GetResService();
            ResSource=await getResService.GetRes(SelectServer, SelectBBS, SelectKey);
            //public async Task<ObservableCollection<Res>> GetRes(string server, string bbs, string key, GetMode mode = GetMode.LocalRemote)
        }
        
    }
}
