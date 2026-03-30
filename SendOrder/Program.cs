// SendOrder
// Takes in a string command of the format "trader1 BUY 10". Noted in below code

using System.Text;
using RabbitMQ.Client;
using System.Threading;

class Program
{
    static void Main(string[] args) // Input example: "trader1 BUY 100 10.5 XYZ" (Username, BUY/SELL, Quantity, Price, Trading Code)
    {
        if (args.Length != 5)
        {
            Console.WriteLine("Please use the format: <Username> <BUY|SELL> <QTY> <Price> <Trading Code>"); // Instructions for command line input
            return;
        }

        string username = args[0]; // "trader1"
        string side = args[1]; // "BUY" or "SELL"
        int quantity = int.Parse(args[2]);
        double price = double.Parse(args[3]); // "10.0". Prevents invalid input by parsing the price as a double
        string code = args[4]; // Eg. "XYZ"

        string host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";

        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = host // Connect to local instance of RabbitMQ
        };

        IConnection connection = null;

        int retries = 5;
        while (retries > 0) // Starts connection to RabbitMQ, retries every 5 seconds if connection fails, up to 5 times (not an issue in testing for SendOrder)
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

        // Consistent format for message
        string message = $@"
            {{
              ""username"": ""{username}"",
              ""side"": ""{side}"",
              ""quantity"": {quantity},
              ""price"": {price},
              ""code"": ""{code}""
            }}";

        byte[] instruction = Encoding.UTF8.GetBytes(message); // Codes string message into a byte array for transmission

        channel.BasicPublish(exchange: "Trading", routingKey: "Orders", basicProperties: null, body: instruction); // Transmits message to Trading exchange, Orders queue

        Console.WriteLine("Order sent: ");
        Console.WriteLine(message);
        // User can close console

        // Neater implementation, prevents warnings in console every time SendOrder closes
        channel.Close();
        connection.Close();
    }
}