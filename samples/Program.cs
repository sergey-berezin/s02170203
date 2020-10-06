
using System;
using System.Threading.Tasks;
using ImageRecognition;

namespace Launcher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string imagesPath = (args.Length == 0) ? @"D:\Pictires\Desktop" : args[0];

            ImageRecognizer.onnxModelPath = (args.Length == 2) ? args[1]: "resnet34-v1-7.onnx";

            ImageRecognizer.ImageRecognizerResultUpdate += OutputRecognitionHandler;

            await ImageRecognizer.RecognitionAsync(imagesPath);

        }
        static void OutputRecognitionHandler(Prediction s)
        {
            Console.WriteLine($"{s.Path, 80} {s.Label, 15} {s.Confidence, 15}");
        }
    }
}
