using System.CommandLine;
using System.Diagnostics;

namespace AltiumTestTask.TestFileGenerator;

public class Program
{
    private static readonly int _defaultRandomizerSeed = 42;
    private static readonly string _defaultTestFileName = "./testdata.txt";
    private static readonly int _defaultSize = 1024;
    private static readonly SizeCalculationMethod _defaultSizeCalculationMethod = SizeCalculationMethod.MegaBytes;
    private static readonly int _defaultDictionarySize = 100;
    private static readonly int _defaultFileFlushRowCount = 1000;
    
    static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand("Console app to generate a test file.");

        var filePathOption = new Option<FileInfo>(
            name: "--filepath",
            description: "Path to file.");
        filePathOption.SetDefaultValue(new FileInfo(_defaultTestFileName));
        rootCommand.AddOption(filePathOption);

        var sizeCalculationMethodOption = new Option<SizeCalculationMethod>(
            name: "--sizeCalculationMethod",
            description: "Units in which file size will be calculated.");
        sizeCalculationMethodOption.SetDefaultValue(_defaultSizeCalculationMethod);
        rootCommand.AddOption(sizeCalculationMethodOption);
        
        var sizeOption = new Option<int>(
            name: "--size",
            description: "Minimum size value.");
        sizeOption.SetDefaultValue(_defaultSize);
        rootCommand.AddOption(sizeOption);
        
        var seedOption = new Option<int>(
            name: "--seed",
            description: "Randomizer seed.");
        seedOption.SetDefaultValue(_defaultRandomizerSeed);
        rootCommand.AddOption(seedOption);
        
        var dictionarySizeOption = new Option<int>(
            name: "--dictionarySize",
            description: "Amount of word to be used in 'String' part generation.");
        dictionarySizeOption.SetDefaultValue(_defaultDictionarySize);
        rootCommand.AddOption(dictionarySizeOption);

        rootCommand.SetHandler(
            async (file, sizeCalculationMethod, sizeValue, seed, dictionarySize) =>
            {
                var testFileCreator = new TestFileCreator(seed, dictionarySize, _defaultFileFlushRowCount);
                var sizeCalculationOptions = new SizeCalculationOptions(sizeCalculationMethod, sizeValue);
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                
                await testFileCreator.CreateFile(file, sizeCalculationOptions);
                
                stopwatch.Stop();
                Console.WriteLine($"File '{file}' with seed '{seed}' created in '{stopwatch.Elapsed}'.");
            },
            filePathOption,
            sizeCalculationMethodOption,
            sizeOption,
            seedOption,
            dictionarySizeOption);

        await rootCommand.InvokeAsync(args);
    }
}