using Compunet.YoloV8;
using OrigamiPlatform.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OrigamiPlatform.Infrastructure.Services;

public class LocalOnnxImageLabelingService : IImageLabelingService
{
    private readonly string _modelPath;

    private readonly YoloPredictor _predictor;

    public LocalOnnxImageLabelingService()
    {
        _modelPath = Path.Combine(AppContext.BaseDirectory, "MLModels", "yolov8n.onnx");

        if (!File.Exists(_modelPath))
            throw new FileNotFoundException($"Không tìm thấy file YOLO tại: {_modelPath}");

        _predictor = new YoloPredictor(_modelPath);
    }

    public async Task<List<string>> DetectLabelsAsync(Stream imageStream)
    {
        try
        {
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            var result = await _predictor.DetectAsync(imageBytes);

            var detectedLabels = result
                                       .Select(b => b.Name.ToString().ToLower())
                                       .Distinct()
                                       .ToList();

            if (!detectedLabels.Any())
                return new List<string> { "unknown" };

            return detectedLabels;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LỖI AI YOLO]: {ex.Message}");
            return new List<string> { "origami" };
        }
    }
}