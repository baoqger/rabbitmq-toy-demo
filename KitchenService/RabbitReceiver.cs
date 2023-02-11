using Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace KitchenService;
public class RabbitReceiver : IHostedService
{
    private readonly RabbitMqSettings _rabbitSettings;
    private readonly IModel _channel;
    private readonly IHubContext<OrderHub> _orderHub; // for signalr service 


    public RabbitReceiver(RabbitMqSettings rabbitSettings, IModel channel, IHubContext<OrderHub> hub)
    {
        _rabbitSettings = rabbitSettings;
        _channel = channel;
        _orderHub = hub;
    }

    private void DoStuff()
    {
        // declare exchange
        _channel.ExchangeDeclare(exchange: _rabbitSettings.ExchangeName, type: _rabbitSettings.ExchangeType);
        // declare a queue and generate the name
        _channel.QueueDeclare(_rabbitSettings.QueueName);
        Console.WriteLine("Debug: queue - ${0}", _rabbitSettings.QueueName);
        // routing key is #.cookwaffle
        // * (star) can substitute for exactly one word.
        // # (hash) can substitute for zero or more words.
        _channel.QueueBind(queue: _rabbitSettings.QueueName, exchange: _rabbitSettings.ExchangeName, routingKey: "*.cookwaffle");
        // create a consumer
        var consumerAsync = new AsyncEventingBasicConsumer(_channel);
        // message receive handler
        consumerAsync.Received += async (_, ea) =>
        {
            Thread.Sleep(5000);
            var body = ea.Body.ToArray(); // Get the message body from the EventArgs
            var message = Encoding.UTF8.GetString(body); // Decode the body into a string
            Console.WriteLine("Receive message: {0}", message);
            var order = JsonSerializer.Deserialize<Order>(message);

            await _orderHub.Clients.All.SendAsync("new-order", order); // send to single client side
            
            // rabbitmq message receive ack
            // the second parameter is for ack multiple deliveries
            _channel.BasicAck(ea.DeliveryTag, false);
        };
        // start the consumption process.
        // autoAck set to false
        _channel.BasicConsume(queue: _rabbitSettings.QueueName, autoAck: false, consumer: consumerAsync);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        DoStuff();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Dispose();
        return Task.CompletedTask;
    }
}

