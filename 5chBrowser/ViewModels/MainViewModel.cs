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
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<BoardList> boardSource = new ObservableCollection<BoardList>();
        [ObservableProperty]
        public ObservableCollection<ThreadList> threadSource = new ObservableCollection<ThreadList>();
        [ObservableProperty]
        public ObservableCollection<Res> resSource = new ObservableCollection<Res>();

        public MainViewModel()
        {
            //暫定
            Task.Run(async () =>
            {
                await Load();
                if (BoardSource.Count == 0)
                    GetBoardList();
            });
        }
        private async Task Load()
        {
            var service = new SaveStateService();
            var state = await service.Load();
            BoardSource = state.BoardList;
            ThreadSource = state.ThreadList;
            ResSource = state.ResList;
        }
        private async Task Save()
        {
            var service = new SaveStateService();
            var state = new State();
            state.BoardList = BoardSource;
            state.ThreadList = ThreadSource;
            state.ResList = ResSource;
            await service.Save(state);
        }
        private async void GetBoardList()
        {
            GetBoardService getBoardService = new GetBoardService();
            BoardSource = await getBoardService.GetBoard();

            await Save(); //暫定
        }
        [RelayCommand]
        public async void SelectBoard(TreeViewItemInvokedEventArgs args)
        {
            var node = args.InvokedItem as BoardList;
            if (node == null)
            { return; }

            GetThreadService getThreadService = new GetThreadService();
            ThreadSource = await getThreadService.GetThread(node);

            await Save(); //暫定
        }
        [RelayCommand]
        public async void SelectThread(SelectionChangedEventArgs args)
        {
            var selectItem = args.AddedItems.Cast<ThreadList>().FirstOrDefault();
            if (selectItem == null)
            { return; }

            GetResService getResService = new GetResService();
            ResSource = await getResService.GetRes(selectItem);

            await Save(); //暫定
        }

    }
}
