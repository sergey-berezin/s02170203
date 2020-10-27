using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Collections.Specialized;
using Microsoft.WindowsAPICodePack.Dialogs;
using ImageRecognizerViewModel;
using ImageRecognition;
using System.IO;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace wpfTest
{
    public partial class MainWindow : Window
    {
        public delegate void OutputHandler(Blank b);
        public static event OutputHandler ImageReady;

        private ImageRecognizerVM imageRecognizer;
        private PictireObservable pictires;

        public MainWindow()
        {
            ImageRecognizer.ImageRecognizerResultUpdate += CreateImage;
            ImageReady += Add;

            imageRecognizer = new ImageRecognizerVM();
            pictires = new PictireObservable();
            
            InitializeComponent();
            Labels.DataContext = pictires;
            PictiresPanel.DataContext = pictires;
            this.DataContext = imageRecognizer;
        }

//===========================================================================================//
        
        private void OpenImages(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.ShowDialog();
            try
            {
                imageRecognizer.ImagesPath = dialog.FileName;
            }
            catch (Exception)
            {
                MessageBox.Show("Директория не выбрана.");
            }
        }

        private void OpenOnnxModel(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Onnx Model (*.onnx)|*.onnx";
            openFileDialog.ShowDialog();
            try
            {
                imageRecognizer.OnnxModelPath = openFileDialog.FileName;
            }
            catch (Exception)
            {
                MessageBox.Show("Файл не выбран.");
            }
        }

//===========================================================================================//
        
        private async void Control(object sender, ExecutedRoutedEventArgs e)
        {
            if (imageRecognizer.IsRunning) //stop recognition
            {
                controlButton.IsEnabled = false;
                await imageRecognizer.Stop();
                controlButton.IsEnabled = true;
                controlButton.Content = "Start";

        }
            else //start recognition
            {
                controlButton.Content = "Stop";
                pictires.Clear();
                PictiresPanel.DataContext = null;
                try
                {
                    await imageRecognizer.Start();
                }

                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show($"Директория {imageRecognizer.ImagesPath} не найдена");
                    controlButton.Content = "Start";
                }
                catch (Microsoft.ML.OnnxRuntime.OnnxRuntimeException s)
                {
                    MessageBox.Show($"Bad allocation.\n Нехватка RAM. Выберите другую модель или уменьшите количсетво файлов.");
                    controlButton.Content = "Start";
                }
            }
        }

//===========================================================================================//
        
        private void Add(Blank s)
        {                              
            App.Current.Dispatcher.Invoke(() =>
            {
                var l = (from pic in pictires
                         where pic.Label == s.Label
                         select pic).FirstOrDefault();
                
                if (l == null) //first time 
                {
                    pictires.Add(new Pictire(s.Label, new Image()
                    {
                        Source = s.Image,
                    }));
                }
                else
                {
                    int index = pictires.IndexOf(l);
                    pictires[index].Count++;
                    pictires[index].Images.Add(new Image()
                    {
                        Source = s.Image,
                    });
                }

                imageRecognizer.ImagesCounter++;
                
                if (imageRecognizer.ImagesCount == imageRecognizer.ImagesCounter)
                {
                    imageRecognizer.IsRunning = false;
                    controlButton.Content = "Start";
                }
            });                      
        }

        private async Task ImageGenerationAsync(Prediction p)
        {
            await Task.Factory.StartNew((pred) =>
            {
                Prediction prediction = (Prediction)pred;
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(imageRecognizer.ImagesPath + "\\" + prediction.Path);
                bmp.DecodePixelWidth = 800;
                bmp.EndInit();
                bmp.Freeze();
                ImageReady(new Blank()
                {
                    Image = bmp,
                    Label = prediction.Label,
                }); ;                    
            }, p);
        }

        private async void CreateImage(Prediction p)
        {
            await ImageGenerationAsync(p);
        }


//===========================================================================================//

        private void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((Pictire)Labels.SelectedItem != null)
            {
                PictiresPanel.DataContext = (Pictire)Labels.SelectedItem;
            }
        }
    }

    public struct Blank
    {
        public BitmapImage Image;
        public string Label;
    }

    internal class Pictire : BaseViewModel
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
        
        public ObservableCollection<Image> Images { get; set; }

        public Pictire(string s, Image i)
        {
            Label = s;
            Images = new ObservableCollection<Image>();
            Images.Add(i);
            count = 1;
        }

    }

    internal class PictireObservable : ObservableCollection<Pictire>, INotifyPropertyChanged { }
}

