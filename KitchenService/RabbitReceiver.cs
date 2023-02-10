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
        var queueName = _channel.QueueDeclare().QueueName;
        Console.WriteLine("Debug: queue - ${0}", queueName);
        // routing key is #.cookwaffle
        _channel.QueueBind(queue: queueName, exchange: _rabbitSettings.ExchangeName, routingKey: "#.cookwaffle");
        // create a consumer
        var consumerAsync = new AsyncEventingBasicConsumer(_channel);
        // message receive handler
        consumerAsync.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray(); // Get the message body from the EventArgs
            var message = Encoding.UTF8.GetString(body); // Decode the body into a string
            var order = JsonSerializer.Deserialize<Order>(message);

            await _orderHub.Clients.All.SendAsync("new-order", order); // send to single client side
            Console.WriteLine("Receive message: ${0}", message);
            // rabbitmq message receive ack
            _channel.BasicAck(ea.DeliveryTag, false);
        };
        // start the consumption process.
        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumerAsync);
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

