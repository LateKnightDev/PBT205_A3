using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange
{
    public class Order // Class representing an order received by the exchange, split into the information required
    {
        public string Username { get; set; } // Username
        public string Side { get; set; } // "BUY" or "SELL"
        public int Quantity { get; set; } // Fixed at 100 for this assessment
        public double Price { get; set; } // Price specified by the user in the command line input
        public string Code { get; set; } // Trading company code
    }
}
