using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _5chBrowser.Models
{
    public class BoardInfo
    {
        //public string description { get; set; }
        public IList<Menu_List> menu_list { get; set; }
        //public int last_modify { get; set; }
        //public string last_modify_string { get; set; }

        public class Menu_List
        {
            public IList<Category_Content> category_content { get; set; }
            public string category_name { get; set; }
            //public int category_total { get; set; }
            //public string category_number { get; set; }
        }

        public class Category_Content
        {
            //public string category_name { get; set; }
            //public int category_order { get; set; }
            //public int category { get; set; }
            public string url { get; set; }
            public string board_name { get; set; }
            //public string directory_name { get; set; }
        }

    }
}
