using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.IO.Pipelines;

namespace System.Text.Json.Stream.Benchmarks;

[MemoryDiagnoser]
public class WriteToBufferStreamBenchmark
{
    private ReadOnlySequence<byte> _singleSegmentBuffer;
    private ReadOnlySequence<byte> _multiSegmentBuffer;
    private PipeWriter? _writer;
    private MemoryStream? _stream;
    
    private const int BufferSize = 8192;
    private const int SmallSize = 256;

    [GlobalSetup]
    public void Setup()
    {
        // Setup single segment buffer
        var singleData = new byte[BufferSize];
        Random.Shared.NextBytes(singleData);
        _singleSegmentBuffer = new ReadOnlySequence<byte>(singleData);
        
        // Setup multi-segment buffer (simulate real-world fragmented data)
        var segments = new List<ReadOnlyMemory<byte>>();
        for (int i = 0; i < 8; i++)
        {
            var segmentData = new byte[BufferSize / 8];
            Random.Shared.NextBytes(segmentData);
            segments.Add(segmentData);
        }
        _multiSegmentBuffer = CreateMultiSegmentSequence(segments);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _stream = new MemoryStream();
        _writer = PipeWriter.Create(_stream);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        // Don't complete or dispose here - let them be garbage collected
        // Completing the writer can affect the stream state
        _writer = null;
        _stream = null;
    }

    [Benchmark(Baseline = true)]
    public void OriginalApproach_SingleSegment()
    {
        var slice = _singleSegmentBuffer.Slice(0, SmallSize);
        
        // Original approach
        slice.CopyTo(_writer!.GetSpan((int)slice.Length));
        _writer.Advance((int)slice.Length);
    }

    [Benchmark]
    public void OptimizedApproach_SingleSegment()
    {
        var slice = _singleSegmentBuffer.Slice(0, SmallSize);
        
        // Optimized approach
        if (slice.IsSingleSegment)
        {
            ReadOnlySpan<byte> sourceSpan = slice.FirstSpan;
            Span<byte> destinationSpan = _writer!.GetSpan((int)slice.Length);
            sourceSpan.CopyTo(destinationSpan);
            _writer.Advance((int)slice.Length);
        }
    }

    [Benchmark]
    public void OriginalApproach_MultiSegment()
    {
        var slice = _multiSegmentBuffer.Slice(0, BufferSize);
        
        // Original approach
        slice.CopyTo(_writer!.GetSpan((int)slice.Length));
        _writer.Advance((int)slice.Length);
    }

    [Benchmark]
    public void OptimizedApproach_MultiSegment()
    {
        var slice = _multiSegmentBuffer.Slice(0, BufferSize);
        
        // Optimized approach
        if (slice.IsSingleSegment)
        {
            ReadOnlySpan<byte> sourceSpan = slice.FirstSpan;
            Span<byte> destinationSpan = _writer!.GetSpan((int)slice.Length);
            sourceSpan.CopyTo(destinationSpan);
            _writer.Advance((int)slice.Length);
        }
        else
        {
            foreach (ReadOnlyMemory<byte> segment in slice)
            {
                if (segment.Length > 0)
                {
                    ReadOnlySpan<byte> sourceSpan = segment.Span;
                    Span<byte> destinationSpan = _writer!.GetSpan(sourceSpan.Length);
                    sourceSpan.CopyTo(destinationSpan);
                    _writer.Advance(sourceSpan.Length);
                }
            }
        }
    }

    [Benchmark]
    public void SlowApproach_ToArray()
    {
        var slice = _singleSegmentBuffer.Slice(0, SmallSize);
        
        // Your commented "slower" approach
        _writer!.Write(slice.ToArray());
    }

    private static ReadOnlySequence<byte> CreateMultiSegmentSequence(List<ReadOnlyMemory<byte>> segments)
    {
        if (segments.Count == 0)
            return ReadOnlySequence<byte>.Empty;

        if (segments.Count == 1)
            return new ReadOnlySequence<byte>(segments[0]);

        var first = new BufferSegment(segments[0]);
        var current = first;

        for (int i = 1; i < segments.Count; i++)
        {
            current = current.Append(segments[i]);
        }

        return new ReadOnlySequence<byte>(first, 0, current, current.Memory.Length);
    }

    private class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public BufferSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new BufferSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }
}
