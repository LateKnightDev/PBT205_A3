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

// PopulateDB.Run(); // Run once to create and populate the database with sample data, comment out after first run
// QueryDB.Run(); // Run this along with return; below to query the DB
// return;

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
    byte[] instruction = each.Body.ToArray(); // Receives the instruction as a byte array (from RabbitMQ)
    string message = Encoding.UTF8.GetString(instruction); // Converts the byte array back to a string
    Order? order = JsonSerializer.Deserialize<Order>(message, new JsonSerializerOptions{PropertyNameCaseInsensitive = true}); // Converts to Order object (temporary storage)
    if (order == null) // Little fix for a warning CS8600. May be null and cause a crash
    {
        Console.WriteLine("Invalid order received");
        return;
    }
    bool tradeExecuted = false;
    Trade? executedTrade = null;
    ManageDB.QueryOrders(order, out tradeExecuted, out executedTrade); // Queries the database for matching orders and executes trade or adds to correct order table
    if (tradeExecuted)
    {
        PublishTrade(executedTrade);
        tradeExecuted = false;
        executedTrade = null;
    }
};
channel.BasicConsume(queue: "Orders", autoAck: true, consumer: consumer); // Listening for messages on the Orders queue
Console.WriteLine("Exchange is running..."); // Message to acknowledge this program is running
Thread.Sleep(Timeout.Infinite); // Used with Docker
void PublishTrade(Trade? trade) // Publishes message to RabbitMQ
{
    string message = JsonSerializer.Serialize(trade);
    byte[] instruction = Encoding.UTF8.GetBytes(message);
    channel.BasicPublish(exchange: "Trading", routingKey: "Trades", basicProperties: null, body: instruction);
}