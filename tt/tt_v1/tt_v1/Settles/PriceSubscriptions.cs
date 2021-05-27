using System.Linq;
using System.Collections.Generic;

namespace Subscriber
{
    public class PriceSubscriptions
    {
        IEnumerable<string> _defaultFields;
        public IEnumerable<string> DefaultFields
        {
            get { return _defaultFields; }
            set { _defaultFields = value.Distinct(); }
        }

        public IEnumerable<Product> Products { get; set; }
    }
}
