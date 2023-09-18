namespace BusMessageWriter;

public interface IBusMessageWriterSettings
{
	int WriteWatingTimeSec { get; }
	int MaxBufferSize { get; }
}
