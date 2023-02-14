using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace _5chBrowser.Models
{
    public class Res
    {
        public int No { get; set; }
        public string Name { get; set; }
        public string Mail { get; set; }
        public string Message { get; set; }
        private string _options;
        public string Options
        {
            get => _options;
            set
            {
                if (_options == value)
                    return;
                Date = GetDate(value);
                ID = idRegex.Match(value).Groups[1].Value;
                _options = value;
            }
        }
        //[JsonIgnore]
        public DateTime? Date { get;private set; }
        //[JsonIgnore]
        public string ID { get;private set; }

        private static Regex idRegex = new Regex(@"ID:([^<\s]+)");
        private static Regex dateRegex = new Regex(@"\d+/\d+/\d+\(.+?\)?(\s+\d+:\d+:\d+(\.\d+)?)?");

        [JsonConstructor]
        public Res()
        { }

        public Res(int no, string name, string mail, string options, string message)
        {
            No = no;
            Name = name;
            Mail = mail;
            Options = options;
            Message = message;
        }

        private DateTime? GetDate(string options)
        {
            try
            {
                return DateTime.Parse(dateRegex.Match(options).Value);
            }
            catch
            {
                return null;
            }
        }
    }
}
