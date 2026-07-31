using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OrigamiPlatform.Application.Interfaces;

public interface IImageLabelingService
{
    Task<List<string>> DetectLabelsAsync(Stream imageStream);
}