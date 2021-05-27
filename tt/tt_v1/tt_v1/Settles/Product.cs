using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subscriber
{
    public class Product
    {
        public string Exchange { get; set; }
        public string Type { get; set; }
        public string Code { get; set; }
        public IEnumerable<string> ExtraFields { get; set; }
    }
}
