using Microsoft.Extensions.Logging.Abstractions;

namespace BusMessageWriter.Test
{
	public class BusMessageWriterTest
	{
		[Theory]
		[InlineData(10, 10)]
		[InlineData(1, 10)]
		[InlineData(1000, 500)]
		[InlineData(999, 1)]
		[InlineData(50, 1000)]
		[InlineData(100, 100)]
		public async Task SendMessageAsync(int attempts, int dataSize)
		{
			var bus = new MoqBusConnection(TimeSpan.FromMilliseconds(10));
			var logger = new NullLogger<BusMessageWriter>();
			var settings = new BusMessageWriterSettings
			{
				MaxBufferSize = 1000,
				WriteWatingTimeSec = 1
			};

			using var writer = new BusMessageWriter(settings, bus, logger);

			var data = new byte[dataSize];
			var tasks = Enumerable.Range(0, attempts)
				.AsParallel()
				.Select(i => writer.SendMessageAsync(data));

			await Task.WhenAll(tasks);

			await Task.Delay(TimeSpan.FromSeconds(settings.WriteWatingTimeSec).Add(TimeSpan.FromSeconds(1)));

			Assert.Equal(attempts * dataSize, bus.WritedBytes);
		}
	}
}