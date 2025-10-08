using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.IO.Pipelines;

namespace System.Text.Json.Stream.Benchmarks;

/// <summary>
/// Benchmark to compare the old vs new WriteToBufferStream implementations
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class WriteToBufferComparisonBenchmark
{
    private ReadOnlySequence<byte> _smallSingleSegment;
    private ReadOnlySequence<byte> _largeSingleSegment;
    private ReadOnlySequence<byte> _smallMultiSegment;
    private ReadOnlySequence<byte> _largeMultiSegment;
    
    private PipeWriter? _writer;
    private MemoryStream? _stream;

    [Params(256, 1024, 4096, 16384)]
    public int BufferSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Single segment buffers
        var smallData = new byte[256];
        var largeData = new byte[BufferSize];
        Random.Shared.NextBytes(smallData);
        Random.Shared.NextBytes(largeData);
        
        _smallSingleSegment = new ReadOnlySequence<byte>(smallData);
        _largeSingleSegment = new ReadOnlySequence<byte>(largeData);
        
        // Multi-segment buffers
        _smallMultiSegment = CreateMultiSegmentBuffer(256, 4);
        _largeMultiSegment = CreateMultiSegmentBuffer(BufferSize, 8);
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

    // Original implementation benchmarks
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Small", "SingleSegment", "Original")]
    public void Original_Small_SingleSegment()
    {
        var slice = _smallSingleSegment;
        
        // Original approach
        slice.CopyTo(_writer!.GetSpan((int)slice.Length));
        _writer.Advance((int)slice.Length);
    }

    [Benchmark]
    [BenchmarkCategory("Large", "SingleSegment", "Original")]
    public void Original_Large_SingleSegment()
    {
        var slice = _largeSingleSegment;
        
        // Original approach
        slice.CopyTo(_writer!.GetSpan((int)slice.Length));
        _writer.Advance((int)slice.Length);
    }

    [Benchmark]
    [BenchmarkCategory("Small", "MultiSegment", "Original")]
    public void Original_Small_MultiSegment()
    {
        var slice = _smallMultiSegment;
        
        // Original approach
        slice.CopyTo(_writer!.GetSpan((int)slice.Length));
        _writer.Advance((int)slice.Length);
    }

    [Benchmark]
    [BenchmarkCategory("Large", "MultiSegment", "Original")]
    public void Original_Large_MultiSegment()
    {
        var slice = _largeMultiSegment;
        
        // Original approach
        slice.CopyTo(_writer!.GetSpan((int)slice.Length));
        _writer.Advance((int)slice.Length);
    }

    // Optimized implementation benchmarks
    [Benchmark]
    [BenchmarkCategory("Small", "SingleSegment", "Optimized")]
    public void Optimized_Small_SingleSegment()
    {
        var slice = _smallSingleSegment;
        WriteToBufferStreamOptimized(slice);
    }

    [Benchmark]
    [BenchmarkCategory("Large", "SingleSegment", "Optimized")]
    public void Optimized_Large_SingleSegment()
    {
        var slice = _largeSingleSegment;
        WriteToBufferStreamOptimized(slice);
    }

    [Benchmark]
    [BenchmarkCategory("Small", "MultiSegment", "Optimized")]
    public void Optimized_Small_MultiSegment()
    {
        var slice = _smallMultiSegment;
        WriteToBufferStreamOptimized(slice);
    }

    [Benchmark]
    [BenchmarkCategory("Large", "MultiSegment", "Optimized")]
    public void Optimized_Large_MultiSegment()
    {
        var slice = _largeMultiSegment;
        WriteToBufferStreamOptimized(slice);
    }

    // Slow approach for comparison
    [Benchmark]
    [BenchmarkCategory("Small", "SingleSegment", "Slow")]
    public void Slow_Small_ToArray()
    {
        var slice = _smallSingleSegment;
        _writer!.Write(slice.ToArray());
    }

    private void WriteToBufferStreamOptimized(ReadOnlySequence<byte> slice)
    {
        if (slice.Length <= 0) return;
        
        if (slice.IsSingleSegment)
        {
            // Fast path: direct span copy for single segment
            ReadOnlySpan<byte> sourceSpan = slice.FirstSpan;
            Span<byte> destinationSpan = _writer!.GetSpan((int)slice.Length);
            sourceSpan.CopyTo(destinationSpan);
            _writer.Advance((int)slice.Length);
            return;
        }
        
        // Multi-segment case
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

    private ReadOnlySequence<byte> CreateMultiSegmentBuffer(int totalSize, int segmentCount)
    {
        var segments = new List<ReadOnlyMemory<byte>>();
        int segmentSize = totalSize / segmentCount;
        
        for (int i = 0; i < segmentCount; i++)
        {
            var size = (i == segmentCount - 1) ? totalSize - (segmentSize * i) : segmentSize;
            var data = new byte[size];
            Random.Shared.NextBytes(data);
            segments.Add(data);
        }
        
        return CreateMultiSegmentSequence(segments);
    }

    private static ReadOnlySequence<byte> CreateMultiSegmentSequence(List<ReadOnlyMemory<byte>> segments)
    {
        if (segments.Count == 0) return ReadOnlySequence<byte>.Empty;
        if (segments.Count == 1) return new ReadOnlySequence<byte>(segments[0]);

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