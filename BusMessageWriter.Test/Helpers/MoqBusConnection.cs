namespace BusMessageWriter.Test;

internal class MoqBusConnection : IBusConnection
{
	public int WritedBytes { get; private set; }
	public TimeSpan SendDelay { get; private set; }

	public MoqBusConnection(TimeSpan sendDelay)
	{
		SendDelay = sendDelay;
	}

	public async Task PublishAsync(byte[] data)
	{
		await Task.Delay(SendDelay);
		WritedBytes += data.Length;
	}
}
