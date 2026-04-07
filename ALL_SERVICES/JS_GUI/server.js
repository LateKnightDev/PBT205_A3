const express = require('express');
const amqp = require('amqplib');

const app = express();
const port = 3000;

// Middleware to parse JSON
app.use(express.json());
app.use(express.static('.')); // Serve static files from current directory

let channel = null;

// Connect to RabbitMQ with retry
async function connectRabbitMQ(retryCount = 0) {
    try {
        const connection = await amqp.connect('amqp://localhost:5672');
        channel = await connection.createChannel();

        // Declare exchange
        await channel.assertExchange('Trading', 'direct', { durable: true });

        // Declare queue
        await channel.assertQueue('Orders', { durable: true });

        // Bind queue to exchange
        await channel.bindQueue('Orders', 'Trading', 'Orders');

        console.log('Connected to RabbitMQ');
    } catch (error) {
        console.error(`Failed to connect to RabbitMQ (attempt ${retryCount + 1}):`, error.message);
        if (retryCount < 10) { // Retry up to 10 times
            setTimeout(() => connectRabbitMQ(retryCount + 1), 5000); // Wait 5 seconds before retry
        }
    }
}

// Send order to RabbitMQ
async function sendOrder(order) {
    if (!channel) {
        throw new Error('RabbitMQ not connected. Please try again later.');
    }

    const message = JSON.stringify(order);
    channel.publish('Trading', 'Orders', Buffer.from(message));
    console.log('Order sent:', message);
}

// Routes
app.get('/', (req, res) => {
    res.sendFile(__dirname + '/index.html');
});

app.post('/send-order', async (req, res) => {
    try {
        const order = req.body;

        // Validate order
        if (!order.username || !order.side || !order.quantity || !order.price || !order.code) {
            return res.status(400).json({ error: 'All fields are required' });
        }

        if (!['BUY', 'SELL'].includes(order.side.toUpperCase())) {
            return res.status(400).json({ error: 'Side must be BUY or SELL' });
        }

        if (!Number.isInteger(order.quantity) || order.quantity <= 0) {
            return res.status(400).json({ error: 'Quantity must be a positive integer' });
        }

        if (typeof order.price !== 'number' || order.price <= 0) {
            return res.status(400).json({ error: 'Price must be a positive number' });
        }

        // Ensure side is uppercase
        order.side = order.side.toUpperCase();

        await sendOrder(order);
        res.json({ success: true, message: 'Order sent successfully' });
    } catch (error) {
        console.error('Error sending order:', error);
        res.status(500).json({ error: 'Failed to send order' });
    }
});

// Start server
app.listen(port, async () => {
    console.log(`Server running at http://localhost:${port}`);
    await connectRabbitMQ();
});