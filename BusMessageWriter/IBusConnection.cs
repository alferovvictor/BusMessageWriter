namespace BusMessageWriter;

public interface IBusConnection
{
	Task PublishAsync(byte[] data);
}