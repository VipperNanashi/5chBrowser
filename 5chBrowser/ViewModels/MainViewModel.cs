using _5chBrowser.Services;
using _5chBrowser.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _5chBrowser.ViewModels
{
    public partial class MainViewModel:ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<BoardList> boardSource=new ObservableCollection<BoardList>();

        public MainViewModel()
        {
            GetBoardList();
        }
        private async void GetBoardList()
        {
            GetBoardService getBoardService= new GetBoardService();
            BoardSource = await getBoardService.GetBoard();
        }
    }
}
