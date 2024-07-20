// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using MergeSortTestTask.Benchmark;

var summary = BenchmarkRunner.Run<BulkTextReaderBenchmark>();