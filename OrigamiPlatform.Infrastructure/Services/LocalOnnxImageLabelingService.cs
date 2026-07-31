//using Microsoft.ML.OnnxRuntime;
//using Microsoft.ML.OnnxRuntime.Tensors;
//using OrigamiPlatform.Application.Interfaces;
//using OrigamiPlatform.Domain.Constants; // Gọi thư viện chứa file AiDictionary
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;
//using SixLabors.ImageSharp.Processing;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;

//namespace OrigamiPlatform.Infrastructure.Services;

//public class LocalOnnxImageLabelingService : IImageLabelingService
//{
//    private readonly string _modelPath = string.Empty;
//    private readonly string[] _labels = Array.Empty<string>();

//    public LocalOnnxImageLabelingService()
//    {
//        _modelPath = Path.Combine(AppContext.BaseDirectory, "MLModels", "mobilenetv2-7.onnx");
//        var labelPath = Path.Combine(AppContext.BaseDirectory, "MLModels", "word.txt");

//        if (!File.Exists(_modelPath))
//            throw new FileNotFoundException($"Không tìm thấy file AI Model tại thư mục chạy: {_modelPath}");

//        if (File.Exists(labelPath))
//        {
//            _labels = File.ReadAllLines(labelPath);
//        }
//    }

//    public async Task<List<string>> DetectLabelsAsync(Stream imageStream)
//    {
//        try
//        {
//            // 1. Đọc và resize ảnh thực tế về chuẩn 224x224 của MobileNet
//            using var image = await Image.LoadAsync<Rgb24>(imageStream);
//            image.Mutate(x => x.Resize(new ResizeOptions
//            {
//                Size = new Size(224, 224),
//                Mode = ResizeMode.Crop
//            }));

//            // 2. Chuyển ảnh thành ma trận Tensor chuẩn hóa màu sắc
//            var input = new DenseTensor<float>(new[] { 1, 3, 224, 224 });
//            for (int y = 0; y < image.Height; y++)
//            {
//                for (int x = 0; x < image.Width; x++)
//                {
//                    var pixel = image[x, y];
//                    input[0, 0, y, x] = ((pixel.R / 255f) - 0.485f) / 0.229f;
//                    input[0, 1, y, x] = ((pixel.G / 255f) - 0.456f) / 0.224f;
//                    input[0, 2, y, x] = ((pixel.B / 255f) - 0.426f) / 0.225f;
//                }
//            }

//            // 3. Chạy AI offline cục bộ
//            var inputs = new List<NamedOnnxValue>
//            {
//                NamedOnnxValue.CreateFromTensor("data", input)
//            };

//            using var session = new InferenceSession(_modelPath);
//            using var results = session.Run(inputs);

//            var output = results.First().AsEnumerable<float>().ToArray();
//            var maxIndex = Array.IndexOf(output, output.Max());

//            // Xử lý an toàn tránh lỗi Index Out of Bounds
//            var rawLabel = (_labels.Length > maxIndex && maxIndex >= 0) ? _labels[maxIndex].ToLower() : "unknown";

//            var simplifiedLabels = new List<string>();
//            bool matched = false;

//            // 4. Gọi Dictionary dùng chung để map từ khóa
//            foreach (var map in AiDictionary.KeywordMapping)
//            {
//                if (rawLabel.Contains(map.Key))
//                {
//                    simplifiedLabels.Add(map.Value);
//                    matched = true;
//                }
//            }

//            // Nếu không có trong bảng quy hoạch, trả về từ gốc để dự phòng
//            if (!matched)
//            {
//                simplifiedLabels.Add(rawLabel.Split(',').First().Trim());
//            }

//            return simplifiedLabels.Distinct().ToList();
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"[LỖI AI OFFLINE]: {ex.Message}");
//            return new List<string> { "origami" };
//        }
//    }
//}
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

    // Đã đổi tên Class từ YoloV8 sang YoloPredictor chuẩn với thư viện
    private readonly YoloPredictor _predictor;

    public LocalOnnxImageLabelingService()
    {
        // Trỏ đường dẫn tới file YOLOv8
        _modelPath = Path.Combine(AppContext.BaseDirectory, "MLModels", "yolov8n.onnx");

        if (!File.Exists(_modelPath))
            throw new FileNotFoundException($"Không tìm thấy file YOLO tại: {_modelPath}");

        // Khởi tạo YoloPredictor
        _predictor = new YoloPredictor(_modelPath);
    }

    public async Task<List<string>> DetectLabelsAsync(Stream imageStream)
    {
        try
        {
            // Chuyển Stream ảnh thành mảng byte để đưa vào YOLO
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            // YOLO quét ảnh (Phát hiện vật thể)
            var result = await _predictor.DetectAsync(imageBytes);

            // Thêm .ToString() để chuyển Span thành String bình thường
            var detectedLabels = result
                                       .Select(b => b.Name.ToString().ToLower())
                                       .Distinct()
                                       .ToList();

            // Nếu không tìm thấy gì
            if (!detectedLabels.Any())
                return new List<string> { "unknown" };

            // Trả về kết quả chung chung (ví dụ: ["dog"])
            return detectedLabels;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LỖI AI YOLO]: {ex.Message}");
            return new List<string> { "origami" };
        }
    }
}