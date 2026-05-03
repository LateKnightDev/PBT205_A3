using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Retaining for the purpose of posting Trade information to RabbitMQ

namespace Exchange
{
    public class Trade // Class representing a trade that was made, split into the required information
    {
        public string Buyer { get; set; } // Username of the buyer
        public string Seller { get; set; } // Username of the seller
        public int Quantity { get; set; } // Updated to be variable for A3
        public double Price { get; set; } // Price it was traded at
        public string Code { get; set; } // Trading company code
        public DateTime Timestamp { get; set; } // Added a timestamp recording when the trade was made
    }
}