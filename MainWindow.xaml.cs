
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

//using DataBaseSetup;
using ImageRecognizerViewModel;
using ImageRecognition;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WPF
{
    public partial class MainWindow : Window
    {

        private ImageRecognizerVM imageRecognizer;

        public MainWindow()
        {
            ImageRecognizer.Result += Add;

            imageRecognizer = new ImageRecognizerVM();

            InitializeComponent();

            using (var db = new DataBaseSetup.Context())
            {
                foreach (var r in db.Recognitions.Include(a => a.Photos).ThenInclude(a => a.Pixels))
                {
                    ObservableCollection<Photo> a = new ObservableCollection<Photo>();
                    foreach (var photo in r.Photos)
                    {
                        a.Add(new Photo
                        {
                            IsSavedInDataBase = true,
                            Path = photo.Path,
                            Pixels = null,
                            Image = ByteToImage(photo.Pixels.Pixels)
                        }); ;
                    }
                    imageRecognizer.Recognitions.Add(new Recognition
                    {
                        Title = r.Title,
                        Count = r.Photos.Count,
                        Photos = a
                    });
                }
            }          
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
                imageRecognizer.ImagesPath = dialog.FileName ?? imageRecognizer.ImagesPath;
            }
            catch { }
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
                //PictiresPanel.DataContext = null;
                try
                {
                    imageRecognizer.IsRunning = true;
                    await Task.Run(async () => imageRecognizer.Photos = await ImageGenerator(imageRecognizer.ImagesPath));
                    await imageRecognizer.Start();
                    imageRecognizer.IsRunning = false;
                }

                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show($"Директория {imageRecognizer.ImagesPath} не найдена", "Ошибка");                    
                }
                catch (Microsoft.ML.OnnxRuntime.OnnxRuntimeException s)
                {
                    MessageBox.Show($"{s.Message}", "Ошибка");                   
                }
                //catch (Exception s)
                //{
                //    MessageBox.Show($"{s.Message}", "Ошибfgfка");
                //}
                finally
                {
                    imageRecognizer.IsRunning = false;
                    imageRecognizer.IsStopping = false;
                }
            }
        }

        private async void ClearStorage(object sender, ExecutedRoutedEventArgs e)
        {
            PictiresPanel.DataContext = null;
            await imageRecognizer.Clear();
            imageRecognizer.Recognitions.Clear();
        }

//===========================================================================================//

        private void Add(Prediction prediction)
        {
            string path = imageRecognizer.ImagesPath + "\\" + prediction.Path;

            App.Current.Dispatcher.Invoke(() =>
            {
                var l = (from pic in imageRecognizer.Recognitions
                         where pic.Title == prediction.Label
                         select pic).FirstOrDefault();

                var q = (from pic in imageRecognizer.Photos
                         where path == pic.Path
                         select pic).FirstOrDefault();

                if (l == null) //first time 
                {
                    imageRecognizer.Recognitions.Add(new Recognition
                    {
                        Title = prediction.Label,
                        Count = 1,
                        Photos = new ObservableCollection<Photo> { q }
                    });
                }
                else
                {
                    int index = imageRecognizer.Recognitions.IndexOf(l);
                    imageRecognizer.Recognitions[index].Count++;
                    imageRecognizer.Recognitions[index].Photos.Add((Photo)q);                  
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
            if ((Recognition)Labels.SelectedItem != null)
            {
                PictiresPanel.DataContext = (Recognition)Labels.SelectedItem;
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

        private async Task<List<Photo>> ImageGenerator(string path)
        {
            string[] images = Directory.GetFiles(path);
            Task<Photo>[] tasks = new Task<Photo>[images.Length];

            for (int i = 0; i < images.Length; i++)
            {
                tasks[i] = Task<Photo>.Factory.StartNew((imagePath) =>
                {
                    string path = (string)imagePath;
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path);
                    bmp.DecodePixelHeight = 300;
                    bmp.EndInit();
                    bmp.Freeze();

                    return new Photo
                    {
                        IsSavedInDataBase = false,
                        Path = path,
                        Pixels = ImagetoByte(bmp),
                        Image = bmp
                    };

                }, images[i]);
            }
            await Task.WhenAll(tasks);

            var t = Task<List<Photo>>.Factory.StartNew(() =>
            {
                List<Photo> a = new List<Photo>();
                foreach (Task<Photo> t in tasks)
                {
                    a.Add(t.Result);
                    Trace.WriteLine($"{t.Result.Path} {a.Count}");
                }
                return a;
            });
            return await t;
        }

//===========================================================================================//

    }

    //internal class Pictire : BaseViewModel
    //{
    //    private string label;
    //    public string Label
    //    {
    //        get
    //        {
    //            return label;
    //        }
    //        set
    //        {
    //            label = value;
    //            OnPropertyChanged(nameof(Label));
    //        }
    //    }

    //    private int count;
    //    public int Count
    //    {
    //        get
    //        {
    //            return count;
    //        }
    //        set
    //        {
    //            count = value;
    //            OnPropertyChanged(nameof(Count));
    //        }
    //    }
        
    //    public ObservableCollection<Photo> Photos { get; set; }

    //    public Pictire(string s, string path, BitmapImage i)
    //    {
    //        Label = s;
    //        Photos = new ObservableCollection<Photo>();

    //        Photos.Add(new Photo
    //        {
    //            Path = path,
    //            Image = i
    //        });
    //        count = 1;
    //    }

    //    public Pictire() { }

    //    public override string ToString()
    //    {
    //        string s = "";
    //        s += Label;
    //        s += "  ";
    //        s += Count.ToString();
    //        foreach(var a in Photos)
    //        {
    //            s += a.Path + "\n";
    //        }
    //        return s;
    //    }

    //}
    //internal struct Photo
    //{
    //    public string Path { get; set; }
    //    public BitmapImage Image { get; set; }

    //}

    //internal class PictireObservable : ObservableCollection<Pictire>, INotifyPropertyChanged { }
}

