
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ImageRecognizerViewModel;
using ImageRecognition;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace wpfTest
{
    public partial class MainWindow : Window
    {

        private ImageRecognizerVM imageRecognizer;
        private PictireObservable pictires;

        public MainWindow()
        {
            ImageRecognizer.Result += Add;

            imageRecognizer = new ImageRecognizerVM();
            pictires = new PictireObservable();
            
            InitializeComponent();
            Labels.DataContext = pictires;
            PictiresPanel.DataContext = pictires;            
            DataContext = imageRecognizer;
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
                MessageBox.Show("Директория не выбрана.", "Ошибка");
            }
        }

        private void OpenOnnxModel(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Onnx Model (*.onnx)|*.onnx";
            openFileDialog.ShowDialog();
            try
            {
                imageRecognizer.OnnxModelPath = openFileDialog.FileName;
            }
            catch (Exception)
            {
                MessageBox.Show("Файл не выбран.", "Ошибка");
            }
        }

//===========================================================================================//
        
        private async void Control(object sender, ExecutedRoutedEventArgs e)
        {
            if (imageRecognizer.IsRunning) //stop recognition
            {
                try
                {
                    await imageRecognizer.Stop();
                }
                catch (Microsoft.ML.OnnxRuntime.OnnxRuntimeException s)
                {
                    MessageBox.Show($"{s.Message}", "Ошибка");
                }
                finally
                {
                    imageRecognizer.IsRunning = false;
                    imageRecognizer.IsStopping = false;
                }
            }
            else //start recognition
            {
                pictires.Clear();
                PictiresPanel.DataContext = null;
                try
                {
                    await imageRecognizer.Start();
                }

                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show($"Директория {imageRecognizer.ImagesPath} не найдена", "Ошибка");                    
                }
                catch (Microsoft.ML.OnnxRuntime.OnnxRuntimeException s)
                {
                    MessageBox.Show($"{s.Message}", "Ошибка");                   
                }
                catch (Exception)
                {
                    MessageBox.Show("В каталоге не только фотогорафии", "Ошибка");
                }
                finally
                {
                    imageRecognizer.IsRunning = false;
                    imageRecognizer.IsStopping = false;
                }
            }
        }

//===========================================================================================//
        
        private void Add(Prediction prediction)
        {
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(imageRecognizer.ImagesPath + "\\" + prediction.Path);
            bmp.DecodePixelWidth = 800;
            bmp.EndInit();
            bmp.Freeze();

            App.Current.Dispatcher.Invoke(() =>
            {
                var l = (from pic in pictires
                         where pic.Label == prediction.Label
                         select pic).FirstOrDefault();
                
                if (l == null) //first time 
                {
                    pictires.Add(new Pictire(prediction.Label, new Image()
                    {
                        Source = bmp,
                    }));
                }
                else
                {
                    int index = pictires.IndexOf(l);
                    pictires[index].Count++;
                    pictires[index].Images.Add(new Image()
                    {
                        Source = bmp,
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

//===========================================================================================//

        private void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((Pictire)Labels.SelectedItem != null)
            {
                PictiresPanel.DataContext = (Pictire)Labels.SelectedItem;
            }
        }
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

