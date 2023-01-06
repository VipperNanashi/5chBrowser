using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace _5chBrowser.Models
{
    public class Res
    {
        public int No { get; }
        public string Name { get; }
        public string Mail { get; }
        public string Message { get; }
        public string Options { get; }
        public DateTime? Date { get; }
        public string ID { get; }

        private static Regex idRegex = new Regex(@"ID:([^<\s]+)");
        private static Regex dateRegex = new Regex(@"\d+/\d+/\d+\(.+?\)?(\s+\d+:\d+:\d+(\.\d+)?)?");

        public Res(int no, string name, string mail, string options, string message)
        {
            No = no;
            Name = name;
            Mail = mail;
            Options = options;
            Date = GetDate(options);
            ID = idRegex.Match(options).Groups[1].Value;
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
