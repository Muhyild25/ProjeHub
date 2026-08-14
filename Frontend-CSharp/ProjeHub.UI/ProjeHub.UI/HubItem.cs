using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjeHub.UI
{
    public class HubItem
    {
        public int id { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string category { get; set; }
        public string priority { get; set; }
        public string status { get; set; }
        public string notes { get; set; }
    }
}