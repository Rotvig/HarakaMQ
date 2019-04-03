:disappointed_relieved: The message queue is currently not working at the moment I am working on fixing it. I am working on this branch https://github.com/Rotvig/HarakaMQ/tree/add_unit_tests :disappointed_relieved:


# HarakaMQ
Reliable Message Oriented Middleware Based on UDP

# Guarantees
* Message Delivery
* Order of messages across distributed brokers
* Eventual Consistency

# Development
* Fail over when a broker goes down in a cluster setup
* Reconciliation when a broker rejoins a cluster
* Broker and client auto discovery in a cluster setup
* Add a new broker without restart of a cluster

# Nuget
## HarakaMQ.Client
https://www.nuget.org/packages/HarakaMQ.Client/

# Message Broker

Start up the Broker by building the MessageBroker project, and runs it with the command "dotnet HarakaMQ.MessageBroker.dll".
When running in a cluster setup have a seperate folder with the builded dll's for each broker you have. 
Remember to add a "settings.json" file in the running directory with this content:

```json
{  
   "BrokerPort":11100,
   "PrimaryNumber":1,
   "AntiEntropyMilliseonds":1000,
   "RunInCLusterSetup":false,
   "Brokers":[]
}
```

# Publish

```csharp
var factory = new ConnectionFactory {HostName = "127.0.0.1", ListenPort = 11000, Port = 11100};
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare("hello");
    channel.BasicPublish("hello", Encoding.UTF8.GetBytes("Hello world"));

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}
```

# Subscribe

```csharp
var factory = new ConnectionFactory {HostName = "127.0.0.1", ListenPort = 12000, Port = 11100};
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare("hello");
    var consumer = new DefaultBasicConsumer(channel);
    consumer.Received += (model, ea) =>
    {
        Console.WriteLine(Encoding.UTF8.GetString(ea.Body));
    };
    channel.BasicConsume("hello", consumer);

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}
```
# Test Repo
https://github.com/Rotvig/HarakaMQ-Benchmark
