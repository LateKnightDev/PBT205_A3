import pika

# region fns
#--------------------------------------------------------------------------------------------------------------------------------------------------
#Connect to RabbitMQ on localhost
def connect_to_middleware():
    connection = pika.BlockingConnection(pika.ConnectionParameters(host='localhost'))
    
    #make the channel, which will be used to send messages 
    channel = connection.channel()                    #program pauses until connection is made because it is 'blocking' (not async/event driven)
          
    return connection, channel

#create message and send it through a channel
def send_routed_message(channel, _routing_key, message):
    channel.basic_publish(      #publish a message
    exchange='Trading',         #using the exchange defined in 'connect_to_middleware()'. If the exchange doesn't exist, rabbitmq creates it
    routing_key= _routing_key,  #this is the queue where the exchange will send the message
    body=message)               #the message content

    print("message sent using routing key: ", _routing_key)
    
#binds a queue to the routing key. using the exchange 'Trading' defined above
def bind_queue(channel, _queue, _routing_key):
    channel.queue_bind(exchange='Trading', queue=_queue, routing_key=_routing_key)

#runs callback fn when a message comes in
def subscribe_to_queue(channel, queue_name, callback):
    channel.basic_consume(queue=queue_name, on_message_callback=callback, auto_ack=True)  #automatically acknowledge receipt
#--------------------------------------------------------------------------------------------------------------------------------------------------
# endregion
# region classes
#--------------------------------------------------------------------------------------------------------------------------------------------------
class Publisher():
    def __init__(self):
        self.connection, self.channel = connect_to_middleware() #connect to the middleware

    def declare_queue(self, _queue):
        #declare queue. If it already exists, rabbitmq won't create it again
        self.channel.queue_declare(queue=_queue, durable=False)  #'durable=true' the queue will also survive a reboot like the exchange
                
    def bind_queue(self, queue, _routing_key):
        bind_queue(self.channel, queue, _routing_key)
        
    def publish(self, routing_key, message):
        #send a message to the exchange + routing key to sort it into a queue
        send_routed_message(self.channel, routing_key, message) 

    def end(self):
        self.connection.close()

#--------------------------------------------------------------------------------------------------------------------------------------------------
class Subscriber():
    def __init__(self):
        self.connection, self.channel = connect_to_middleware() #connect to the middleware
        self.publisher = None                                   #also has a pulisher object it can use to send messages

    #same fns as Publisher
    def declare_queue(self, _queue):
        self.channel.queue_declare(queue=_queue, durable=False)          
    def bind_queue(self, queue, _routing_key):
        self.routing_key = _routing_key
        bind_queue(self.channel, queue, _routing_key)

    def subscribe_to_queue(self, queue_name, callback):
        self.channel.basic_consume(queue=queue_name, on_message_callback=callback, auto_ack=True) 

    def start_listening(self):
        self.channel.start_consuming()

    def end(self):
        self.channel.stop_consuming()
        self.connection.close()

# endregion
