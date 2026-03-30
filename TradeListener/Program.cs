// TradeListener GUI interface, takes published trades from RabbitMQ and displays them in the console

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TradeListener;
using System.Threading;

Console.WriteLine("Trade Listener started...");
Console.WriteLine("Waiting for trades...\n");

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

channel.QueueDeclare(queue: "Trades", durable: true, exclusive: false, autoDelete: false, arguments: null); // Checks queue Trades exists on RabbitMQ

EventingBasicConsumer consumer = new EventingBasicConsumer(channel); // Open channel to listen for messages on Trades queue

consumer.Received += (sender, each) =>
{
    byte[] instruction = each.Body.ToArray(); // Receives the instruction as a byte array (from Trades queue)
    string message = Encoding.UTF8.GetString(instruction); // Converts the byte array back to a string
    Trade? trade = JsonSerializer.Deserialize<Trade>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // Converts to Trade object
    if (trade == null)
    {
        Console.WriteLine("Invalid trade message received"); // Shouldn't happen but I'm clearing a warning message
        return;
    }

    // Console.Clear(); // Was deleting Buyer and Seller info
    Console.WriteLine($"Last Trade Price: {trade.Price}");
    Console.WriteLine($"Quantity: {trade.Quantity}");
    Console.WriteLine($"Buyer: {trade.Buyer}");
    Console.WriteLine($"Seller: {trade.Seller}");
    Console.WriteLine($"Trading Company Code: {trade.Code}");
    Console.WriteLine($"Time (UTC): {trade.Timestamp}");
};
channel.BasicConsume(queue: "Trades", autoAck: true, consumer: consumer);
// Console.ReadLine(); // Used in testing, not with Docker

Thread.Sleep(Timeout.Infinite); // Used with Docker