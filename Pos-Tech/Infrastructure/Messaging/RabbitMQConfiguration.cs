using RabbitMQ.Client;
using System.Collections.Generic;

namespace Infrastructure.Messaging
{
	public static class RabbitMQConfiguration
	{
		public const string ExchangeName = "contacts-exchange";
		public const string QueueName = "contacts-queue";
		public const string DeadLetterQueueName = "contacts-dlq";
		public const string RoutingKey = "create";

		public static void DeclareQueuesAndExchanges(IModel channel)
		{
			channel.QueueDeclare(
				queue: DeadLetterQueueName,
				durable: true,
				exclusive: false,
				autoDelete: false
			);

			channel.ExchangeDeclare(
				exchange: ExchangeName,
				type: ExchangeType.Direct,
				durable: true
			);

			channel.QueueDeclare(
				queue: QueueName,
				durable: true,
				exclusive: false,
				autoDelete: false,
				arguments: new Dictionary<string, object>
				{
					{ "x-dead-letter-exchange", "" },
					{ "x-dead-letter-routing-key", DeadLetterQueueName }
				}
			);

			channel.QueueBind(
				queue: QueueName,
				exchange: ExchangeName,
				routingKey: RoutingKey
			);
		}

		public static ConnectionFactory CreateConnectionFactory()
		{
			return new ConnectionFactory
			{
				HostName = "rabbitmq",
				Port = 5672,
				UserName = "guest",
				Password = "guest"
			};
		}
	}
}