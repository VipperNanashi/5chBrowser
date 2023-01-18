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

namespace _5chBrowser.ViewModels
{
    public partial class MainViewModel:ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<BoardList> boardSource=new ObservableCollection<BoardList>();
        [ObservableProperty]
        public ObservableCollection<ThreadList>threadSource=new ObservableCollection<ThreadList>();
        
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
            GetThreadService getThreadService= new GetThreadService();
            ThreadSource = await getThreadService.GetThread(threadURL.ToString());
        }
    }
}
