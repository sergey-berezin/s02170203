
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

using DataBaseSetup;
using ImageRecognizerViewModel;
using ImageRecognition;
using Microsoft.EntityFrameworkCore;

namespace WPF
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

            using (var db = new Context())
            {
                foreach (var r in db.Recognitions.Include(a => a.Photo).ThenInclude(a => a.Pixels))
                {
                    ObservableCollection<Photo> a = new ObservableCollection<Photo>();
                    foreach (var photo in r.Photo)
                    {
                        a.Add(new Photo
                        {
                            Path = photo.Path,
                            Image = ByteToImage(photo.Pixels.Pixels)
                        });
                    }
                    pictires.Add(new Pictire()
                    {
                        Label = r.Title,
                        Count = r.Count,
                        Photos = a
                    });
                }
            }

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
                catch (Exception s)
                {
                    MessageBox.Show($"{s.Message}", "Ошибка");
                }
                finally
                {
                    imageRecognizer.IsRunning = false;
                    imageRecognizer.IsStopping = false;
                }
            }
        }

        private async void ClearStorage(object sender, ExecutedRoutedEventArgs e)
        {
            Trace.WriteLine("AAA");
            await imageRecognizer.Clear();
            pictires.Clear();
        }

//===========================================================================================//

        private void Add(Prediction prediction)
        {
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(imageRecognizer.ImagesPath + "\\" + prediction.Path);
            bmp.DecodePixelWidth = 80;
            bmp.EndInit();
            bmp.Freeze();

            string path = imageRecognizer.ImagesPath + "\\" + prediction.Path;

            App.Current.Dispatcher.Invoke(() =>
            {
                var l = (from pic in pictires
                         where pic.Label == prediction.Label
                         select pic).FirstOrDefault();

                var q = (from pic in imageRecognizer.Files
                         where pic.Recognition == prediction.Label
                         select pic).FirstOrDefault();
                
                if (q == null)
                {
                    imageRecognizer.Files.Add(new FileBlank(prediction.Label, path, ImagetoByte(bmp)));
                }
                else 
                {
                    int index = imageRecognizer.Files.IndexOf(q);
                    imageRecognizer.Files[index].Count++;
                    imageRecognizer.Files[index].Photos.Add(new ImageRecognizerViewModel.Photo
                    {
                        Path = path,
                        Pixels = ImagetoByte(bmp),

                    });
                }
                if (l == null) //first time 
                {
                    pictires.Add(new Pictire(prediction.Label, path, bmp));
                }
                else
                {
                    int index = pictires.IndexOf(l);
                    pictires[index].Count++;
                    pictires[index].Photos.Add(new Photo
                    {
                        Path = path,
                        Image = bmp
                    });                       
                }

                imageRecognizer.ImagesCounter++;

                if (imageRecognizer.ImagesCount == imageRecognizer.ImagesCounter)
                {
                    imageRecognizer.IsRunning = false;
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

//===========================================================================================//

        private BitmapImage ByteToImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new Exception("null");
            }
            //return await Task<BitmapImage>.Run(() =>
            //{              
                var image = new BitmapImage();
                using (var mem = new MemoryStream(imageData))
                {
                    mem.Position = 0;
                    image.BeginInit();
                    image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = null;
                    image.StreamSource = mem;
                    image.EndInit();
                }
                image.Freeze();
                return image;
            //});
        }

        private byte[] ImagetoByte(BitmapImage bmp)
        {
            byte[] data;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = ms.ToArray();
            }
            return data;
        }

//===========================================================================================//

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
        
        public ObservableCollection<Photo> Photos { get; set; }

        public Pictire(string s, string path, BitmapImage i)
        {
            Label = s;
            Photos = new ObservableCollection<Photo>();

            Photos.Add(new Photo
            {
                Path = path,
                Image = i
            });
            count = 1;
        }

        public Pictire() { }

        public override string ToString()
        {
            string s = "";
            s += Label;
            s += "  ";
            s += Count.ToString();
            foreach(var a in Photos)
            {
                s += a.Path + "\n";
            }
            return s;
        }

    }
    internal struct Photo
    {
        public string Path { get; set; }
        public BitmapImage Image { get; set; }

    }

    internal class PictireObservable : ObservableCollection<Pictire>, INotifyPropertyChanged { }
}

