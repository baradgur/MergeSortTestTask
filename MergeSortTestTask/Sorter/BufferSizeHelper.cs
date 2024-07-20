using System.Numerics;

namespace MergeSortTestTask.Sorter;

public static class BufferSizeHelper
{
    public static int MinBufferSize => 1024 * 1024 * 32; //32 MB

    public static int EstimateBufferSize()
    {
        var availableMemory = GC.GetTotalMemory(false);
        var memoryInfo = GC.GetGCMemoryInfo();
        // we estimate max degree of parallelism as Environment.ProcessorCount
        // since the computation when sorting is pretty heavy,
        // we want to use as many parallel thread as possible
        var maxMemoryByProcessorCount
            = GetPowerOfTwoLessThanOrEqualTo(
                (memoryInfo.HighMemoryLoadThresholdBytes - availableMemory) / Environment.ProcessorCount);

        //we will merge 2 buffers in memory into 1 writer
        var maxBufferSizeByMemoryAndProcessorCount = GetPowerOfTwoLessThanOrEqualTo(maxMemoryByProcessorCount / 3);

        return Math.Max(maxBufferSizeByMemoryAndProcessorCount, MinBufferSize);
    }

    private static int GetPowerOfTwoLessThanOrEqualTo(long x)
    {
        return x <= 0 ? 0 : 1 << (int) Math.Log(x, 2);
    }
}