using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace _5chBrowser.Models
{
    public class BoardList
    {
        public string BoardTitle { get; set; }
        public string BoardURL { get; set; }
        public ObservableCollection<BoardList> Children { get; set; } = new ObservableCollection<BoardList>();
    }
}
