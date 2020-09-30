
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ImageRecognition;

namespace Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            string imagesPath = (args.Length == 0) ? @"D:\Code\C#\7\prac\images" : args[0];

            ImageRecognizer.onnxModelPath = (args.Length == 2) ? args[1]: "resnet152-v2-7.onnx";

            ImageRecognizer.ImageRecognizerResultUpdate += OutputRecognitionHandler;

            Task t = new Task(() => ImageRecognizer.Recognition(imagesPath));

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();
            t.Start();
            //Thread.Sleep(2000);
            //ImageRecognizer.CancelRecognition();
            t.Wait();
            sw1.Stop();

            Stopwatch sw2 = new Stopwatch();
            sw2.Start();
            ImageRecognizer.RecognitionConsistently(imagesPath);
            sw2.Stop();

            Console.WriteLine($"MultiTask Time: {sw1.ElapsedMilliseconds}");
            Console.WriteLine($"MultiTask Time: {sw2.ElapsedMilliseconds}");
        }
        static void OutputRecognitionHandler(string s)
        {
            Console.WriteLine(s);
        }

    }
}
