// Exchange

/*
ACKNOWLEDGEMENT OF AI TOOL USAGE
I acknowledge that the following AI tool has been used in the creation of this assessment: 
ChatGPT by OpenAI (https://chat.openai.com/). It was used as a guide for generating the Dockerfiles and docker-compose.yml, in support of the RabbitMQ interface
I confirm that the use of the AI tool has been in accordance with the relevant policies. 
I confirm that the final output is authored by me and represents my own thoughts and critical analysis. 
I take full responsibility for the final content of this assessment. 
Luke Dawson - StudentID A00113129
*/

using Exchange;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;

// List<Order> buyOrders = new List<Order>(); // Data structure for storing unmatched buy orders
// List<Order> sellOrders = new List<Order>(); // Data structure for storing unmatched sell orders

Dictionary<string, List<Order>> buyOrders = new Dictionary<string, List<Order>>(); // New data container for multiple trading companies
Dictionary<string, List<Order>> sellOrders = new Dictionary<string, List<Order>>(); // New data container for multiple trading companies

string host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";

ConnectionFactory factory = new ConnectionFactory
{
    HostName = host // Connect to local instance of RabbitMQ
};

IConnection connection = null;

int retries = 5;
while (retries > 0)
{
    try
    {
        connection = factory.CreateConnection();
        break;
    }
    catch
    {
        Console.WriteLine("RabbitMQ not ready, retrying in 5 seconds...");
        Thread.Sleep(5000);
        retries--;
    }
}

if (connection == null)
{
    throw new Exception("Could not connect to RabbitMQ.");
}

// using IConnection connection = factory.CreateConnection(); // TCP connection to RabbitMQ server
using IModel channel = connection.CreateModel(); // Virtual connection (channel) for communication with RabbitMQ

// The 5 lines below are for building RabbitMQ setup from scratch
channel.ExchangeDeclare(exchange: "Trading", type: ExchangeType.Direct, durable: true); // Create exchange named Trading in clean setup of RabbitMQ
channel.QueueDeclare(queue: "Orders", durable: true, exclusive: false, autoDelete: false, arguments: null); // Declare Orders queue to receive orders from SendOrder
channel.QueueDeclare(queue: "Trades", durable: true, exclusive: false, autoDelete: false, arguments: null); // Declare Trades queue to publish executed trades for TradeListener
channel.QueueBind(queue: "Orders", exchange: "Trading", routingKey: "Orders"); // Bind Orders queue to Trading exchange with routing key "Orders"
channel.QueueBind(queue: "Trades", exchange: "Trading", routingKey: "Trades"); // Bind Trades queue to Trading exchange with routing key "Trades"

EventingBasicConsumer consumer = new EventingBasicConsumer(channel); // Open channel to listen for messages on Orders queue

consumer.Received += (sender, each) =>
{
    byte[] instruction = each.Body.ToArray(); // Receives the instruction as a byte array (from SendOrder)
    string message = Encoding.UTF8.GetString(instruction); // Converts the byte array back to a string
    Order? order = JsonSerializer.Deserialize<Order>(message, new JsonSerializerOptions{PropertyNameCaseInsensitive = true}); // Converts to Order object
    if (order == null) // Little fix for a warning CS8600. May be null and cause a crash
    {
        Console.WriteLine("Invalid order received");
        return;
    }
    ProcessOrder(order);
};
channel.BasicConsume(queue: "Orders", autoAck: true, consumer: consumer); // Listening for messages on the Orders queue

Console.WriteLine("Exchange is running..."); // Message to acknowledge this program is running
// Console.ReadLine(); // Required to keep the program running without closing (but not when using Docker)

Thread.Sleep(Timeout.Infinite); // Used with Docker

void ProcessOrder(Order order)
{
    Console.WriteLine($"Processing {order.Side} {order.Quantity} @ {order.Price} for {order.Code}"); // Debugging message, delete later

    // If there are no previous trades under that trading company code:
    if (!buyOrders.ContainsKey(order.Code))
        buyOrders[order.Code] = new List<Order>();

    if (!sellOrders.ContainsKey(order.Code))
        sellOrders[order.Code] = new List<Order>();

    if (order.Side == "BUY")
    {
        Order? matchingSell = sellOrders[order.Code] // Looking for a matching sell order from the list of sellOrders
            .Where(sellPrice => sellPrice.Price <= order.Price) // Find a sell order with a price less than or equal to the buy order price
            .OrderBy(sellPrice => sellPrice.Price) // If more than one match, sort by lowest price first
            .FirstOrDefault(); // Take the first match (lowest price) or null if no match

        if (matchingSell != null) // Trade match found
        {
            sellOrders[order.Code].Remove(matchingSell); // Remove the matched sell order from the list of unmatched sell orders
            Trade trade = new Trade // Create a new trade object with the details of the executed trade
            {
                Buyer = order.Username,
                Seller = matchingSell.Username,
                Quantity = order.Quantity,
                Price = matchingSell.Price,
                Code = order.Code,
                Timestamp = DateTime.UtcNow
            };
            PublishTrade(trade); // Publish the trade to the Trades queue on RabbitMQ
            Console.WriteLine($"TRADE EXECUTED @ {matchingSell.Price}"); // Debugging
        }
        else // No trade match found, add to the list of unmatched buy orders
        {
            buyOrders[order.Code].Add(order);
            buyOrders[order.Code] = buyOrders[order.Code].OrderByDescending(buyPrice => buyPrice.Price).ToList();
            Console.WriteLine("BUY order added to book"); // Debugging
        }
    }
    else if (order.Side == "SELL")
    {
        Order? matchingBuy = buyOrders[order.Code] // Looking for a matching buy order from the list of buyOrders
            .Where(buyPrice => buyPrice.Price >= order.Price) // Find a buy order with a price greater than or equal to the sell order price
            .OrderByDescending(buyPrice => buyPrice.Price) // If more than one match, sort by highest price first
            .FirstOrDefault(); // Take the first match (highest price) or null if no match

        if (matchingBuy != null) // Trade match found
        {
            buyOrders[order.Code].Remove(matchingBuy); // Remove the matched buy order from the list of unmatched buy orders
            Trade trade = new Trade // Create a new trade object with the details of the executed trade
            {
                Buyer = matchingBuy.Username,
                Seller = order.Username,
                Quantity = order.Quantity,
                Price = matchingBuy.Price,
                Code = order.Code,
                Timestamp = DateTime.UtcNow
            };
            PublishTrade(trade); // Publish the trade to the Trades queue on RabbitMQ
            Console.WriteLine($"TRADE EXECUTED @ {matchingBuy.Price}"); // Debugging
        }
        else // No trade match found, add to the list of unmatched sell orders
        {
            sellOrders[order.Code].Add(order);
            sellOrders[order.Code] = sellOrders[order.Code].OrderBy(sellPrice => sellPrice.Price).ToList();
            Console.WriteLine("SELL order added to book"); // Debugging
        }
    }
}
void PublishTrade(Trade trade) // Publishes message to RabbitMQ
{
    string message = JsonSerializer.Serialize(trade);
    byte[] instruction = Encoding.UTF8.GetBytes(message);
    channel.BasicPublish(exchange: "Trading", routingKey: "Trades", basicProperties: null, body: instruction);
    Console.WriteLine($"Trade published @ {trade.Price}"); // Debugging
}