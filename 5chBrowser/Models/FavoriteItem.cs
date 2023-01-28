using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace _5chBrowser.Models
{
    public class FavoriteItem
    {
        public string Name { get; }
        public string CategoryName { get; }
        public string BoardName { get; }
        public string DatKey { get; }

        public FavoriteItem(string name, string categoryName, string boardName, string datKey)
        {
            Name = name;
            CategoryName = categoryName;
            BoardName = boardName;
            DatKey = datKey;
        }
    }
}