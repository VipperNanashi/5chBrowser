using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _5chBrowser.Models
{
    public class ThreadList
    {
        public BoardList Board { get; set; }
        public string ThreadName { get; set; }
        public string ThreadCount { get; set; }
        public string Dat { get; set; }
        public string LastTime { get; set; }
    }
}
