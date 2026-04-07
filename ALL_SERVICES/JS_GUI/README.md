# PBT205 A3 Group Project

## Overview

This project provides a trading system with a GUI frontend and RabbitMQ middleware for order handling. It includes:

- `ALL_SERVICES/Exchange`: C# service for processing order data and interacting with the database.
- `ALL_SERVICES/GUI`: Trading GUI and backend server for sending orders to RabbitMQ.
- `ALL_SERVICES/docker-compose.yml`: Docker Compose setup for RabbitMQ and the Exchange service.

## Key Components

### GUI

The GUI folder contains:

- `index.html`: Web-based order entry form.
- `server.js`: Node.js backend server that receives order submissions and publishes them to RabbitMQ.
- `package.json`: Node.js project definition with dependencies.
- `rabbitmq_logic.py`: Python RabbitMQ helper code used by the original Dear PyGui application.
- `gui.py`: Original Dear PyGui desktop GUI application.

### Middleware

- `rabbitmq`: Runs RabbitMQ using Docker Compose.
- `exchange`: The Exchange service built from the `Exchange` folder.

## Getting Started

### Prerequisites

- Node.js installed
- Docker and Docker Compose installed
- RabbitMQ accessible on `localhost:5672`

### Run the services

1. Open a terminal in `ALL_SERVICES`.
2. Start Docker services:

```bash
docker-compose up -d
```

3. Open a terminal in `ALL_SERVICES/GUI`.
4. Install Node dependencies:

```bash
npm install
```

5. Start the backend server:

```bash
node server.js
```

6. Open the GUI in your browser:

```text
http://localhost:3000
```

## Using the HTML GUI

The web GUI lets you submit orders with the following fields:

- `Username`
- `Buy/Sell`
- `Quantity` (integer)
- `Price` (decimal)
- `Code`

When the form is submitted, the backend sends the order to RabbitMQ using the `Trading` exchange and the `Orders` queue.

## Troubleshooting

- If the server cannot connect to RabbitMQ:
  - Verify RabbitMQ is running in Docker.
  - Confirm port `5672` is mapped and accessible.
  - Restart the Node server after RabbitMQ is available.

- If the web GUI does not load:
  - Confirm the backend server is running at `http://localhost:3000`.
  - Check browser console for any network or JavaScript issues.

## Notes

- The `server.js` file includes retry logic for RabbitMQ connection startup.
- The project supports both the web GUI and the original desktop GUI.
- The RabbitMQ service is configured in `ALL_SERVICES/docker-compose.yml`.
