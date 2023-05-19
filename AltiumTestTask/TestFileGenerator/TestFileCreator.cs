using System.Diagnostics;
using Bogus;

namespace AltiumTestTask.TestFileGenerator;

public class TestFileCreator
{
    private readonly int _seed;
    private readonly int _dictionarySize;
    private readonly int _defaultFileFlushRowCount;
    private readonly int _minWordsInSentence = 1;
    private readonly int _maxWordsInSentence = 4;

    public TestFileCreator(int seed, int dictionarySize, int defaultFileFlushRowCount)
    {
        _seed = seed;
        _dictionarySize = dictionarySize;
        _defaultFileFlushRowCount = defaultFileFlushRowCount;
    }

    public async Task CreateFile(FileInfo file, SizeCalculationOptions sizeCalculationOptions)
    {
        Randomizer.Seed = new Random(_seed);
        
        //we are using a small sictionary to generate data because we need repetitions for the 'String' part.  
        var dictionary = new Faker().Lorem.Words(_dictionarySize);
        var testRowsFaker = new Faker<string>()
            .CustomInstantiator(
                faker => $"{faker.Random.Long()}. " +
                    $"{string.Join(" ", faker.PickRandom(dictionary, faker.Random.Int(_minWordsInSentence, _maxWordsInSentence)))}");

        await using var fileWriter = file.CreateText();

        switch (sizeCalculationOptions.SizeCalculationMethod)
        {
            case SizeCalculationMethod.Rows:
                long generatedCount = 0;
                while (generatedCount < sizeCalculationOptions.SizeValue)
                {
                    var countToGenerate = sizeCalculationOptions.SizeValue - generatedCount >= _defaultFileFlushRowCount
                        ? _defaultFileFlushRowCount
                        : sizeCalculationOptions.SizeValue - generatedCount;
                    var data = testRowsFaker.Generate((int) countToGenerate);
                    var dataString = string.Join("\n", data);
                    await fileWriter.WriteAsync(dataString);
                    generatedCount += _defaultFileFlushRowCount;
                    await fileWriter.FlushAsync();
                }

                break;
            case SizeCalculationMethod.MegaBytes:
                long generatedCountBytes = 0;
                var generatedCountBytesGoal = sizeCalculationOptions.SizeValue * 1024 * 1024;
                while (generatedCountBytes < generatedCountBytesGoal)
                {
                    var data = testRowsFaker.Generate(_defaultFileFlushRowCount);
                    var dataString = string.Join("\n", data);
                    await fileWriter.WriteAsync(dataString);
                    await fileWriter.FlushAsync();
                    generatedCountBytes += dataString.Length;
                }

                break;
            default:
                //unnecessary - SizeCalculationMethod is validated when root command is created
                throw new ArgumentException(nameof(SizeCalculationMethod));
        }

        fileWriter.Close();
    }
}