using Microsoft.Extensions.ObjectPool;
using Serilog;

namespace AltiumTestTask.Sorter;

// public class BulkTextReaderFactory : IBulkTextReaderFactory
// {
//     private readonly ILogger _logger;
//     private readonly IsConcatenationNeededCheck _isConcatenationNeeded;
//     private int _bufferSize;
//
//     public BulkTextReaderFactory(ILogger logger, IsConcatenationNeededCheck isConcatenationNeeded, int bufferSize)
//     {
//         _isConcatenationNeeded = isConcatenationNeeded ?? throw new ArgumentNullException(nameof(isConcatenationNeeded));
//         _bufferSize = bufferSize;
//         _logger = logger;
//     }
//
//     //TODO: use pool of readers?
//     public IBulkTextReader CreateBulkTextReader()
//     {
//         return new BulkTextReader(_logger, _isConcatenationNeeded, _bufferSize);
//     }
// }