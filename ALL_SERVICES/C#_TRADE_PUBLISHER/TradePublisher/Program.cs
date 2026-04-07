using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

Console.WriteLine("TradePublisher starting...");

// Connect to RabbitMQ (Docker exposes it on localhost)
ConnectionFactory factory = new ConnectionFactory()
{
    HostName = "localhost"
};

using IConnection connection = factory.CreateConnection();
using IModel channel = connection.CreateModel();

// Ensure queue exists (safe to declare again)
channel.QueueDeclare(queue: "Trades", durable: true, exclusive: false, autoDelete: false, arguments: null);
Console.WriteLine("Listening for trades...\n");

EventingBasicConsumer consumer = new EventingBasicConsumer(channel);

consumer.Received += (sender, ea) =>
{
    byte[] body = ea.Body.ToArray();
    string message = Encoding.UTF8.GetString(body);

    Console.WriteLine("TRADE RECEIVED:");
    Console.WriteLine(message);
    Console.WriteLine("-------------------------");
};

channel.BasicConsume(queue: "Trades", autoAck: true, consumer: consumer);

// Keep app alive
Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();