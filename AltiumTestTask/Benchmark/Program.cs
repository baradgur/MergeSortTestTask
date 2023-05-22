// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using AltiumTestTask.Benchmark;

var summary = BenchmarkRunner.Run<BulkTextReaderBenchmark>();