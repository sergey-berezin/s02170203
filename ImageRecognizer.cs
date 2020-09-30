
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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

        public delegate void OutputHandler(string s);
        public static event OutputHandler ImageRecognizerResultUpdate;

//===========================================================================================//

        public static string onnxModelPath { get; set; } = "resnet152-v2-7.onnx";
        private static CancellationTokenSource cts { get; set; }
        private static CancellationToken token { get; set; }

//===========================================================================================//

        static ImageRecognizer()
        {
            cts = new CancellationTokenSource();
            token = cts.Token;
        }

//===========================================================================================//

        public static void Recognition(string imagesPath = @"D:\Code\C#\7\prac\images")
        {
            int imagesCount = Directory.GetFiles(imagesPath).Length;
            string[] images = Directory.GetFiles(imagesPath);

            Task[] tasks = new Task[imagesCount];

            try
            {
                for (int i = 0; i < imagesCount; i++)
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
                            throw new OperationCanceledException();
                        }

                        IEnumerable<float> output = results.First().AsEnumerable<float>();
                        float sum = output.Sum(x => (float)Math.Exp(x));
                        IEnumerable<float> softmax = output.Select(x => (float)Math.Exp(x) / sum);

                        IEnumerable<Prediction> top1 = softmax.Select((x, i) => new Prediction { Label = LabelMap.Labels[i], Confidence = x })
                                            .OrderByDescending(x => x.Confidence)
                                            .Take(2);
                        string res = "";
                        foreach (var t in top1)
                        {
                            res += $"{Path.GetFileName((string)imagePath),15} {t.Label,20} {t.Confidence,15}\n";
                        }
                        if (token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }
                        
                        ImageRecognizerResultUpdate?.Invoke(res);

                    }, images[i], token);
                }
                Task.WaitAll(tasks, token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancelled by user.");
            }
        }

        //private void RecognitionMultiTask(string imagesPath)
        //{
        //    int imagesCount = Directory.GetFiles(imagesPath).Length;
        //    string[] images = Directory.GetFiles(imagesPath);

        //    var cts = new CancellationTokenSource();
        //    var token = cts.Token;

        //    Task[] tasks = new Task[imagesCount];

        //    Task cancellationTask = new Task(() =>
        //    {
        //        Console.WriteLine("Press any button to cancel");
        //        Console.ReadKey();
        //        cts.Cancel();
        //    }, token);

        //    try
        //    {
        //        for (int i = 0; i < imagesCount; i++)
        //        {
        //            if (i == 0)
        //            {
        //                cancellationTask.Start();
        //            }

        //            tasks[i] = Task.Factory.StartNew((imagePath) =>
        //            {
        //                return imageTransformation((string)imagePath);

        //            }, images[i], token).ContinueWith(firstTask =>
        //            {
        //                return runRecognition(firstTask.Result, onnxModelPath);

        //            }, token).ContinueWith((secondTask, imagePath) =>
        //            {
        //                return resultExtraction(secondTask.Result, (string)imagePath);

        //            }, images[i], token).ContinueWith(thirdTask =>
        //            {
        //                Notify?.Invoke(thirdTask.Result);
        //            }, token);

        //        }
        //        Task.WaitAll(tasks, token);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        Notify?.Invoke("Cancelled by user.");
        //    }
        //}

        public static void RecognitionConsistently(string imagesPath = @"D:\Code\C#\7\prac\images")
        {
            int imagesCount = Directory.GetFiles(imagesPath).Length;
            string[] images = Directory.GetFiles(imagesPath);

            for (int i = 0; i < imagesCount; i++)
            {
                var res1 = imageTransformation(images[i]);
                var res2 = runRecognition(res1, onnxModelPath);
                var res3 = resultExtraction(res2, images[i]);
                ImageRecognizerResultUpdate?.Invoke(res3);
            }
        }

        public static void CancelRecognition()
        {

            cts.Cancel();

        }

//===========================================================================================//

        private static List<NamedOnnxValue> imageTransformation(string imageDirectory)
        {
            Image<Rgb24> image = Image.Load<Rgb24>(imageDirectory, out IImageFormat format);

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
            return inputs;
        }
        private static IDisposableReadOnlyCollection<DisposableNamedOnnxValue> runRecognition(List<NamedOnnxValue> inputs, string onnxModelDirectory)
        {
            var session = new InferenceSession(onnxModelDirectory);
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
            return results;
        }
        private static string resultExtraction(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results, string image)
        {
            IEnumerable<float> output = results.First().AsEnumerable<float>();
            float sum = output.Sum(x => (float)Math.Exp(x));
            IEnumerable<float> softmax = output.Select(x => (float)Math.Exp(x) / sum);

            IEnumerable<Prediction> top1 = softmax.Select((x, i) => new Prediction { Label = LabelMap.Labels[i], Confidence = x })
                               .OrderByDescending(x => x.Confidence)
                               .Take(2);
            string res = "";
            foreach (var t in top1)
            {
                 res += $"{Path.GetFileName(image),15} {t.Label,20} {t.Confidence,15}\n";
            }

            return res;
        }

//===========================================================================================//   
    }

    internal class Prediction
    {
        public string Label { get; set; }
        public float Confidence { get; set; }
    }
}
