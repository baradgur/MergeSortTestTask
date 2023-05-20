// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using AltiumTestTask.Benchmark;
using AltiumTestTask.TestFileGenerator;
using Defaults = AltiumTestTask.TestFileGenerator.Defaults;

//setup here
// var fileTestFileCreator = new TestFileCreator();
// await fileTestFileCreator.CreateFile(
//     new FileInfo(Defaults.DefaultTestFileName),
//     new SizeCalculationOptions(SizeCalculationMethod.MegaBytes, 100));

//execute
var summary = BenchmarkRunner.Run<DataLineReaderBenchmark>();

//cleanup
File.Delete(Defaults.DefaultTestFileName);