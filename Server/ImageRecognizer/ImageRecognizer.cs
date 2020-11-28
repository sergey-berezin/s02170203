
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageRecognition
{
    public static class ImageRecognizer
    {

//===========================================================================================//

        public delegate void OutputHandler(Prediction s);
        public static event OutputHandler Result;

//===========================================================================================//

        public static string onnxModelPath { get; set; } = "resnet152-v2-7.onnx";
        private static CancellationTokenSource cts { get; set; }
        private static CancellationToken token { get; set; }

        private static Task[] tasks;

//===========================================================================================//

        static ImageRecognizer()
        {
            cts = new CancellationTokenSource();
            token = cts.Token;
        }

//===========================================================================================//

        public static async Task RecognitionAsync(string[] images)
        {                   
            tasks = new Task[images.Length];

            try
            {
                for (int i = 0; i < images.Length; i++)
                {                                   
                    tasks[i] = Task.Factory.StartNew((imagePath) =>
                    {
                        Image<Rgb24> image = Image.Load<Rgb24>((string)imagePath, out IImageFormat format);

                        Stream imageStream = new MemoryStream();
                        image.Mutate(x =>
                        {
                            x.Resize(new ResizeOptions
                            {
                                Size = new Size(224, 224),
                                Mode = ResizeMode.Crop
                            });
                        });
                        image.Save(imageStream, format);

                        Tensor<float> input = new DenseTensor<float>(new[] { 1, 3, 224, 224 });
                        var mean = new[] { 0.485f, 0.456f, 0.406f };
                        var stddev = new[] { 0.229f, 0.224f, 0.225f };
                        for (int y = 0; y < image.Height; y++)
                        {
                            Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                            for (int x = 0; x < image.Width; x++)
                            {
                                input[0, 0, y, x] = ((pixelSpan[x].R / 255f) - mean[0]) / stddev[0];
                                input[0, 1, y, x] = ((pixelSpan[x].G / 255f) - mean[1]) / stddev[1];
                                input[0, 2, y, x] = ((pixelSpan[x].B / 255f) - mean[2]) / stddev[2];
                            }
                        }

                        List<NamedOnnxValue> inputs = new List<NamedOnnxValue>
                        {
                            NamedOnnxValue.CreateFromTensor("data", input)
                        };

                        var session = new InferenceSession(onnxModelPath);
                        IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
                                               
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        IEnumerable<float> output = results.First().AsEnumerable<float>();
                        float sum = output.Sum(x => (float)Math.Exp(x));
                        IEnumerable<float> softmax = output.Select(x => (float)Math.Exp(x) / sum);

                        IEnumerable<Prediction> top1 = softmax.Select((x, i) => new Prediction { Label = LabelMap.Labels[i], Confidence = x })
                                            .OrderByDescending(x => x.Confidence)
                                            .Take(1);
                        Prediction prediction = top1.First();
                        prediction.Path = Path.GetFileName((string)imagePath);

                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        
                        Result?.Invoke(prediction);
                        
                        session.Dispose();
                        image.Dispose();
                        imageStream.Dispose();

                    }, images[i], token);
                }
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException e)
            {
                Trace.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
            }
        }    

        public static async Task CancelRecognitionAsync()
        {
            try
            {
                cts.Cancel();
                await Task.WhenAll(tasks);                
            }
            catch (OperationCanceledException e)
            {
                Trace.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
            }
            finally
            {
                foreach (Task t in tasks)
                {
                    t.Dispose();
                }
                cts.Dispose();
                cts = new CancellationTokenSource();
                token = cts.Token;
            }
        }

//===========================================================================================//   
    }

    public struct Prediction
    {
        public string Path { get; set; }
        public string Label { get; set; }
        public float Confidence { get; set; }
    }
}
