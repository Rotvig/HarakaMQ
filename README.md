# HarakaMQ
Reliable Message Oriented Middleware Based on UDP and created with .NET Core 2.0

# Nuget
## HarakaMQ.Client
https://www.nuget.org/packages/HarakaMQ.Client/

# Message Broker

Start up the Broker by building the MessageBroker project, and runs it with the command "dotnet HarakaMQ.MessageBroker.dll".
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

You can also use the pre-builded Message broker (Client-Server setup) from this project https://github.com/Rotvig/HarakaMQ-Benchmark/tree/master/ClientServerSetup/Broker

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
    var stopWatch = new Stopwatch();
    consumer.Received += (model, ea) =>
    {
        var message = Encoding.UTF8.GetString(ea.Body);
    };
    channel.BasicConsume("hello", consumer);

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}
```
# Test Repo
https://github.com/Rotvig/HarakaMQ-Benchmark
