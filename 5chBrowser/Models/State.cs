using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _5chBrowser.Models
{
    public class State
    {
        public ObservableCollection<BoardList> BoardList { get; set; } = new();
        public ObservableCollection<ThreadList> ThreadList { get; set; } = new();
        public ObservableCollection<Res> ResList { get; set; } = new();
    }
}
