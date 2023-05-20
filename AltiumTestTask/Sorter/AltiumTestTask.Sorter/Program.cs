// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using AltiumTestTask.Sorter;

// var content = "Hello, World!";
// var contentAsSpan = content.AsSpan();
// var contentAsMemory = content.AsMemory();
// contentAsSpan.EnumerateRunes();
// contentAsMemory.TrimEnd();

var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2)); //TODO: to constants or remove
var cancellationToken = cancellationTokenSource.Token;

Console.WriteLine("Hello, World!");

// var list = new List<DataLineWithSeparator>();
// for(int i=0; i < 100; i++)
// {
//     var testString = new string('c', 1024 * 1024);
//     var testClass = new DataLineWithSeparator(testString, 1);
//     list.Add(testClass);
// }


var datareader = new DataLineWithSeparatorReader();

var stopwatch = new Stopwatch();
stopwatch.Start();
var datas = datareader.GetDataMod().ToArray();
stopwatch.Stop();
Console.WriteLine($"reading completed it in {stopwatch.Elapsed}");
stopwatch.Restart();
var lines = File.ReadAllLines("testdata.txt");
stopwatch.Stop();
Console.WriteLine($"reading 2 completed it in {stopwatch.Elapsed}");
foreach (var data in datas)
{
    //if(data.Data.Equals())
}

for (int i = 0; i < lines.Length; i++)
{
    if (!datas[i].Data.Equals(lines[i]))
    {
        Debug.Assert(false,"sssss");
    } 
}



Console.WriteLine($"total dataCount: '{datas.Length}'");
Console.WriteLine($"total linesCount: '{lines.Length}'");



// var buffer = new byte[1024*1024].AsSpan();//megabyte
// using var fileStream = File.Open("testdata.txt", FileMode.Open, FileAccess.Read, FileShare.Read);
// var res = fileStream.Read(buffer)


// //TODO: pick name
// public interface ISeparator
// {
//     Span<string> GetSeparated();
// }

// var inputLines = new BlockingCollection<string>();
// ConcurrentDictionary<long, string> catalog = new ConcurrentDictionary<long, string>();
// ConcurrentBag<string> bag = new ConcurrentBag<string>();
//
// var stopwatch = new Stopwatch();
// stopwatch.Start();
// var readLines = Task.Factory.StartNew(() =>
// {
//     foreach (var line in File.ReadLines("testdata.txt")) 
//         inputLines.Add(line);
//
//     inputLines.CompleteAdding(); 
// });
//
// var processLines = Task.Factory.StartNew(() =>
// {
//     Parallel.ForEach(inputLines.GetConsumingEnumerable(), line =>
//     {
//         // string[] lineFields = line.Split('.');
//         // long id = long.Parse(lineFields[0]);
//         // var name = lineFields[1];
//         // catalog.TryAdd(id, name);
//         bag.Add(line);
//     });
// });
//
// Task.WaitAll(readLines, processLines);
//
// stopwatch.Stop();
//
//Console.WriteLine(stopwatch.Elapsed);
Console.WriteLine("Bye, world");