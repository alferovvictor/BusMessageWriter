using Microsoft.Extensions.Logging;

namespace BusMessageWriter;

public class BusMessageWriter : IDisposable
{
	public int WriteWatingTimeSec { get; }
	private int MaxBufferSize { get; }
	private readonly IBusConnection _connection;
	private readonly MemoryStream _buffer;
	private readonly SemaphoreSlim _sync = new SemaphoreSlim(1);
	private long _timeToSendUtc = DateTime.UtcNow.Ticks;
	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
	private readonly ILogger? _logger; // dispose

	public BusMessageWriter(
		IBusMessageWriterSettings busMessageWriterSettings,
		IBusConnection connection,
		ILogger<BusMessageWriter> logger)
	{
		_connection = connection;
		_logger = logger;
		WriteWatingTimeSec = busMessageWriterSettings.WriteWatingTimeSec;
		MaxBufferSize = busMessageWriterSettings.MaxBufferSize;
		_buffer = new MemoryStream(MaxBufferSize + MaxBufferSize / 10);

		Send(_cancellationTokenSource.Token);
	}

	~BusMessageWriter()
	{
		Dispose();
	}

	private bool _disposed = false;
	public virtual void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_cancellationTokenSource.Cancel();
		GC.SuppressFinalize(this);
		_disposed = true;

		_logger!.LogDebug("Disposed");
	}

	// how to make this method thread safe?
	public async Task SendMessageAsync(byte[] nextMessage)
	{
		if (nextMessage?.Length > 0 is false)
		{
			_logger!.LogWarning("Message is null or empty.");
			return;
		}

		Interlocked.Exchange(ref _timeToSendUtc, DateTime.UtcNow.AddSeconds(WriteWatingTimeSec).Ticks);

		await _sync.WaitAsync();

		try
		{
			_buffer.Write(nextMessage, 0, nextMessage.Length);

			if (_buffer.Length > MaxBufferSize)
			{
				Interlocked.Exchange(ref _timeToSendUtc, DateTime.UtcNow.AddSeconds(-1).Ticks); // it is time to send
			}
		}
		catch (Exception e)
		{
			_logger!.LogError("Write to buffer error.", e);
			throw;
		}
		finally
		{
			_sync.Release();
		}
	}

	private async Task Send(CancellationToken token)
	{
		var data = new MemoryStream(MaxBufferSize * 2);

		while (!token.IsCancellationRequested)
		{
			var timeToSendUtc = Interlocked.Read(ref _timeToSendUtc);
			if (timeToSendUtc > DateTime.UtcNow.Ticks)
			{
				await Task.Delay(0);
				continue;
			}

			await _sync.WaitAsync();

			try
			{
				await _connection.PublishAsync(_buffer.ToArray());
				_buffer.SetLength(0);
			}
			catch (Exception e)
			{
				_logger?.LogError("Bus publish error.", e);
			}
			finally
			{
				_sync.Release();
			}
		}
	}
}
