namespace BusMessageWriter;

public class BusMessageWriterSettings: IBusMessageWriterSettings
{
	public int WriteWatingTimeSec { get; set; } = 2;
	public int MaxBufferSize { get; set; } = 1000;
}
