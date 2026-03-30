using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeListener
{
    public class Trade // Redundant class (its exactly the same as the Trade class in the Exchange project), but used for the GUI
    {
        public string Buyer { get; set; }
        public string Seller { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public string Code { get; set; } // Trading company code
        public DateTime Timestamp { get; set; }
    }
}
