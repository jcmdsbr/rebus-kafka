﻿using Rebus.Activation;
using Rebus.Config;
using Rebus.Kafka;
using Rebus.Routing.TypeBased;
using Scaleout.Messages;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KafkaAsOneWayTransport
{
	class Program
	{
		static void Main(string[] args)
		{
			using (BuiltinHandlerActivator adapter = new BuiltinHandlerActivator(), oneWayAdapter = new BuiltinHandlerActivator())
			{
				var amount = 0;
				Stopwatch sw = Stopwatch.StartNew();

				adapter.Handle<TestMessage>(message =>
				{
					amount = amount + message.MessageNumber;
					Console.WriteLine($"Received : \"{message.MessageNumber}\"");
					if (message.MessageNumber == MessageCount)
						Console.WriteLine($"Получено {MessageCount} сообщений за {sw.ElapsedMilliseconds / 1000f:N3}с");
					return Task.CompletedTask;
				});

				Configure.With(adapter)
					.Logging(l => l.ColoredConsole(Rebus.Logging.LogLevel.Info))
					.Transport(t => t.UseKafka(kafkaEndpoint, nameof(KafkaAsOneWayTransport), "temp"))
					.Start();

				Configure.With(oneWayAdapter)
					.Logging(l => l.ColoredConsole(Rebus.Logging.LogLevel.Info))
					.Transport(t => t.UseKafkaAsOneWayClient(kafkaEndpoint))
					.Routing(r => r.TypeBased().Map<TestMessage>(nameof(KafkaAsOneWayTransport)))
					.Start();

				char key;
				do
				{
					var sendAmount = 0;
					var messages = Enumerable.Range(1, MessageCount)
						.Select(i =>
						{
							sendAmount = sendAmount + i;
							return oneWayAdapter.Bus.Send(new TestMessage { MessageNumber = i });
						}).ToArray();

					Task.WaitAll(messages);
					Console.WriteLine($"Send: {sendAmount} for {sw.ElapsedMilliseconds / 1000f:N3}c");
					Console.WriteLine("Press any key to exit or 'r' to repeat.");
					key = Console.ReadKey().KeyChar;
				} while (key == 'r' || key == 'к');
			}
		}

		const int MessageCount = 10;
		static readonly string kafkaEndpoint = "192.168.0.166:9092";
	}
}