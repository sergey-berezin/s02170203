using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Drawing;
using System.Windows.Input;
using System.Net.Http.Headers;
using System.Threading;
using System.Windows;
using ImageRecognition;
using System.Threading.Tasks;
using System.Linq;

namespace ImageRecognizerViewModel
{
    public class ImageRecognizerVM : BaseViewModel
    {
        public PredictionObservable Predictions { get; private set; }

//===========================================================================================// 

        private string imagesPath = @"D:\Pictires\Desktop";
        public string ImagesPath
        {
            get
            {
                return imagesPath;
            }
            set
            {
                imagesPath = value;
                OnPropertyChanged(nameof(ImagesPath));
            }
        }

        private string onnxModelPath = "resnet34-v1-7.onnx";
        public string OnnxModelPath
        {
            get
            {
                return onnxModelPath;
            }
            set
            {
                onnxModelPath = value;
                OnPropertyChanged(nameof(OnnxModelPath));
            }
        }

//===========================================================================================// 

        private int imagesCount = 1;
        public int ImagesCount
        {
            get
            {
                return imagesCount;
            }
            set
            {
                imagesCount = value;
                OnPropertyChanged(nameof(ImagesCount));
            }
        }

        private int imagesCounter = 0;
        public int ImagesCounter
        {
            get
            {
                return imagesCounter;
            }
            set
            {
                imagesCounter = value;
                OnPropertyChanged(nameof(ImagesCounter));
            }
        }

        public bool IsRunning { get; set; } = false;

//===========================================================================================// 

        public ImageRecognizerVM()
        {
            Predictions = new PredictionObservable();
        }

//===========================================================================================// 

        public void Add(Prediction prediction)
        {
            var l = (from pred in Predictions
                     where pred.Label == prediction.Label
                     select pred).FirstOrDefault();

            if (l == null) //first time 
            {
                Predictions.Add(new PredictionVM(prediction.Label, prediction.Path));
            }
            else
            {
                int index = Predictions.IndexOf(l);
                Predictions[index].Count++;
                Predictions[index].Images.Add(prediction.Path);
            }

            ImagesCounter++;

            if (ImagesCount == ImagesCounter)
            {
                IsRunning = false;
            }

            OnPropertyChanged(nameof(Predictions));
        }

        public async Task Start()
        {
            Predictions.Clear();
            ImagesCount = Directory.GetFiles(ImagesPath).Length;
            ImagesCounter = 0;
            IsRunning = true;
            ImageRecognizer.onnxModelPath = OnnxModelPath;
            await ImageRecognizer.RecognitionAsync(ImagesPath);           
        }

        public async Task Stop()
        {
            await ImageRecognizer.CancelRecognitionAsync();
            IsRunning = false;
        }

//===========================================================================================// 
    }

    public class PredictionVM : BaseViewModel
    {
        private string label;
        public string Label
        {
            get
            {
                return label;
            }
            set
            {
                label = value;
                OnPropertyChanged(nameof(Label));
            }
        }
        
        
        private int count;
        public int Count
        {
            get
            {
                return count;
            }
            set
            {
                count = value;
                OnPropertyChanged(nameof(Count));
            }
        }

        public List<string> Images;

        public PredictionVM(string label, string path)
        {
            Label = label;
            Count = 1;
            Images = new List<string>();
            Images.Add(path);
        }
    }

    public class PredictionObservable : ObservableCollection<PredictionVM>, INotifyPropertyChanged { }

    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
