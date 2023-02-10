using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Messaging;
public static class IServiceCollectionExtensions
{
    // setup the connection to rabbitmq. Shared with the other two projects
    public static IServiceCollection SetUpRabbitMq(this IServiceCollection services, IConfiguration config)
    {
        var configSection = config.GetSection("RabbitMqSettings");
        var settings = new RabbitMqSettings();
        configSection.Bind(settings); // assignment property value by binding
        // add the settings for later use by other classes via injection
        services.AddSingleton<RabbitMqSettings>(settings);


        // As the connection factory is disposable, need to ensure container disposes of it when finished
        // IConnectionFactory and ConnectionFactory are from Rabbitmq.client
        services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory
        {
            HostName = settings.HostName, // rabbitmq connection
            DispatchConsumersAsync = true
        });

        services.AddSingleton<ModelFactory>();
        services.AddSingleton(sp => sp.GetRequiredService<ModelFactory>().CreateChannel());

        return services;
    }

    public class ModelFactory : IDisposable
    {
        private readonly IConnection _connection; // IConnection is for AMQP connection
        private readonly RabbitMqSettings _settings;

        public ModelFactory(IConnectionFactory connectionFactory, RabbitMqSettings settings) {
            _settings = settings;
            _connection = connectionFactory.CreateConnection();
        }

        // rabbitmq model
        public IModel CreateChannel() { 
            var channel = _connection.CreateModel();
            // declare an exchange based AMQP Model
            channel.ExchangeDeclare(exchange: _settings.ExchangeName, type: _settings.ExchangeType);
            return channel;
        }
        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
