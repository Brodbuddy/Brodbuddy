namespace Brodbuddy.TcpProxy.Core;

// Helper class for handling the first chunk of data
public class CompositeStream : Stream
{
    private readonly MemoryStream _initialData;
    private readonly Stream _underlyingStream;
    private bool _initialDataConsumed;

    public CompositeStream(MemoryStream initialData, Stream underlyingStream)
    {
        _initialData = initialData ?? throw new ArgumentNullException(nameof(initialData));
        _underlyingStream = underlyingStream ?? throw new ArgumentNullException(nameof(underlyingStream));
        _initialData.Position = 0; // Ensure we start at the beginning
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => _underlyingStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _underlyingStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_initialDataConsumed) return _underlyingStream.Read(buffer, offset, count);

        var bytesRead = _initialData.Read(buffer, offset, count);

        if (_initialData.Position >= _initialData.Length) _initialDataConsumed = true;

        return bytesRead > 0 ? bytesRead : _underlyingStream.Read(buffer, offset, count);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_initialDataConsumed)
            return await _underlyingStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        var bytesRead = await _initialData.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        if (_initialData.Position >= _initialData.Length) _initialDataConsumed = true;

        if (bytesRead > 0) return bytesRead;
        return await _underlyingStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_initialDataConsumed) return await _underlyingStream.ReadAsync(buffer, cancellationToken);

        var bytesRead = await _initialData.ReadAsync(buffer, cancellationToken);

        if (_initialData.Position >= _initialData.Length) _initialDataConsumed = true;

        if (bytesRead > 0) return bytesRead;
        return await _underlyingStream.ReadAsync(buffer, cancellationToken);
    }


    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => _underlyingStream.Write(buffer, offset, count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _underlyingStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _underlyingStream.WriteAsync(buffer, cancellationToken);
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _initialData.Dispose();
            // We don't own the underlying stream, so don't dispose it
        }

        base.Dispose(disposing);
    }
}