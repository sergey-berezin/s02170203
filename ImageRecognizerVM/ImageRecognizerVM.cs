
using System.ComponentModel;
using System.IO;
using ImageRecognition;
using System.Threading.Tasks;

namespace ImageRecognizerViewModel
{
    public class ImageRecognizerVM : BaseViewModel
    {
        
        public string ControlButtonContent { get; private set; } = "Start";
        public bool ControlButtonEnabled { get; private set; } = true;

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

        private bool isRunning = false;
        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
            set
            {
                ControlButtonContent = value ? "Stop" : "Start";
                isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(ControlButtonContent));
            }
        }

        private bool isStopping = false;
        public bool IsStopping
        {
            get
            {
                return isStopping;
            }
            set
            {
                ControlButtonEnabled = value ? false : true;
                isStopping = value;
                OnPropertyChanged(nameof(IsStopping));
                OnPropertyChanged(nameof(ControlButtonEnabled));
            }
        }

//===========================================================================================// 

        public async Task Start()
        {
            IsRunning = true;           
            ImagesCount = Directory.GetFiles(ImagesPath).Length;
            ImagesCounter = 0;
            ImageRecognizer.onnxModelPath = OnnxModelPath;
            await ImageRecognizer.RecognitionAsync(ImagesPath);
            IsRunning = false;
        }

        public async Task Stop()
        {
            IsStopping = true;
            await ImageRecognizer.CancelRecognitionAsync();
            IsStopping = false;
        }

//===========================================================================================//
    }

    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
