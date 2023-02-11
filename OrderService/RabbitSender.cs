using Messaging;
using Microsoft.Extensions.Options;
using OrderService;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class RabbitSender
{
	private readonly IModel _channel;

	private readonly RabbitMqSettings _rabbitSettings;

	public RabbitSender(RabbitMqSettings rabbitSettings, IModel channel) { 
		_rabbitSettings = rabbitSettings;
		_channel = channel;
	}

	public void PublishMessage<T>(T entity, string key) where T : class { 
		var message = JsonSerializer.Serialize(entity); // serialize to json format

		var body = Encoding.UTF8.GetBytes(message); // encode with UTF8

		// basicPublish
		_channel.BasicPublish(
			exchange: _rabbitSettings.ExchangeName, // send to this exchange
			routingKey: key,                        // routing key
			basicProperties: null,                  
			body: body                              // message body
		);

		Console.WriteLine(" [x] Sent '{0}':'{1}'", key, message);
	}
}