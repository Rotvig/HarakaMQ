# HarakaMQ
Reliable Message Oriented Middleware Based on UDP


# Send

```csharp
var factory = new ConnectionFactory {HostName = "127.0.0.1", ListenPort = 11000, Port = 11100};
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare("hello");
    channel.BasicPublish("hello", Encoding.UTF8.GetBytes("Hello world");

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}
```

# Recieve

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
        Console.WriteLine(msgNumb + " Received {0}", message);
    };
    channel.BasicConsume("hello", consumer);

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}
```
# Test Repo
https://github.com/Rotvig/HarakaMQ-Benchmark
