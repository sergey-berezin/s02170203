
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Contracts;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using Library;
using ServerConnection;

namespace WPF
{
    public class ImageRecognizerVM : BaseViewModel
    {

//===========================================================================================//
        
        public ObservableCollection<Recognition> Recognitions { get; set; }
        public List<Photo> Photos { get; set; }

//===========================================================================================//

        public string ControlButtonContent
        {
            get
            {
                return IsRunning ? "Stop" : "Start";
            }
        }
        public bool ControlButtonEnabled
        {
            get
            {
                return !IsStopping && IsConnected;
            }
        }
        public bool ClearButtonEnabled
        {
            get
            {
                return !IsClearing && IsConnected &&!IsRunning;
            }
        }
        public bool LoadButtonEnabled
        {
            get
            {
                return !IsLoading && IsConnected && !IsRunning;
            }
        }

//===========================================================================================// 

        private string imagesPath = @"D:\Pictires\images";
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

        private string onnxModelPath = @"D:\Downloads\resnet34-v1-7.onnx";
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
                isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(ControlButtonContent));
                OnPropertyChanged(nameof(ClearButtonEnabled));
                OnPropertyChanged(nameof(LoadButtonEnabled));
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
                isStopping = value;
                OnPropertyChanged(nameof(IsStopping));
                OnPropertyChanged(nameof(ControlButtonEnabled));
            }
        }

        private bool isClearing = false;
        public bool IsClearing
        {
            get
            {
                return isClearing;
            }
            set
            {
                isClearing = value;
                OnPropertyChanged(nameof(IsClearing));
                OnPropertyChanged(nameof(ClearButtonEnabled));
            }
        }
       
        private bool isLoading = false;
        public bool IsLoading
        {
            get
            {
                return isLoading;
            }
            set
            {
                isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(LoadButtonEnabled));
            }
        }

        private string colorCircleServerStatus = "red";
        public string ColorCircleServerStatus
        {
            get
            {
                return colorCircleServerStatus;
            }
            set
            {
                colorCircleServerStatus = value;
                OnPropertyChanged(nameof(ColorCircleServerStatus));
            }
        }

        private bool isConnected = false;
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(ClearButtonEnabled));
                OnPropertyChanged(nameof(ControlButtonEnabled));
                OnPropertyChanged(nameof(LoadButtonEnabled));
            }
        }

//===========================================================================================// 

        private string[] images = null;
        private IServer server = null;

//===========================================================================================// 

        public ImageRecognizerVM()
        {          
            server = new Server();
            server.Result += RealTimeAddPrediction;
            server.Connected += () =>
            {
                App.Current.Dispatcher.Invoke(async () =>
                {
                    await LoadAsync();
                    IsConnected = true;
                    ColorCircleServerStatus = "green";
                });
            };
            server.Disconnected += () =>
            {
                IsConnected = false;
                ColorCircleServerStatus = "red";
            };

            Recognitions = new ObservableCollection<Recognition>();
            Photos = new List<Photo>();
        }

//===========================================================================================//

        public async Task StartAsync()
        {
            IsRunning = true;                  

            images = Directory.GetFiles(ImagesPath);
            
            Photos = await GenerateImagesFromPathsAsync();
            Photos = await RemoveRecognizedImagesAsync();
            
            if(Photos.Count == 0)
            {
                IsRunning = false;
                return;
            }

            ImagesCount = Photos.Count;
            ImagesCounter = 0;

            await server.StartAsync(await PrepareDataForSendingAsync(onnxModelPath, Photos));

            IsStopping = true;

            await server.SaveAsync();
            Photos.Clear();
            
            IsStopping = false;
            IsRunning = false;
        }
        public async Task StopAsync()
        {
            IsStopping = true;
            await server.StopAsync();
        }
        public async Task ClearAsync()
        {
            IsClearing = true;

            await server.ClearAsync();
            Recognitions.Clear();
            Photos.Clear();
            ImagesCounter = 0;
            
            IsClearing = false;
        }
        public async Task LoadAsync()
        {
            IsLoading = true;
            if(Recognitions.Count == 0)
            {
                Recognitions.Clear();
                AddRecognitionsToView(await server.LoadAsync());
            }
            IsLoading = false;
        }

//===========================================================================================//

        private async Task<StringContent> PrepareDataForSendingAsync(string onnx, IEnumerable<Photo> photos)
        {
            return await Task.Run(() =>
            {
                List<Photo> sendPhotos = new List<Photo>();
                foreach (var p in photos)
                {
                    sendPhotos.Add(new Photo
                    {
                        IsSavedInDataBase = false,
                        Path = p.Path,
                        Pixels = p.Pixels,
                        Image = null
                    });
                }

                var b = new StartOptions
                {
                    Onnx = onnx,
                    Images = sendPhotos,
                };

                var json = JsonConvert.SerializeObject(b);
                return new StringContent(json, Encoding.UTF8, "application/json");
            });
        }

//===========================================================================================//       

        private void AddRecognitionsToView(List<Recognition> recognitions)
        {
            foreach (var r in recognitions)
            {
                ObservableCollection<Photo> a = new ObservableCollection<Photo>();
                foreach (var photo in r.Photos)
                {
                    a.Add(new Photo
                    {
                        IsSavedInDataBase = true,
                        Path = photo.Path,                       
                        Pixels = null,
                        Image = ByteToImage(Convert.FromBase64String(photo.PixelsString))
                    });
                }
                Recognitions.Add(new Recognition
                {
                    Count = r.Photos.Count,
                    Title = r.Title,
                    Photos = a,
                });
            }
        }
        private void RealTimeAddPrediction(Prediction prediction)
        {
            lock (Recognitions)
            {
                var l = (from pic in Recognitions
                         where pic.Title == prediction.Title
                         select pic).FirstOrDefault();

                var q = (from pic in Photos
                         where pic.Path == prediction.Path
                         select pic).FirstOrDefault();
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (l == null) //first time 
                    {
                        Recognitions.Add(new Recognition
                        {
                            Title = prediction.Title,
                            Count = 1,
                            Photos = new ObservableCollection<Photo> { q }
                        });
                    }
                    else
                    {
                        int index = Recognitions.IndexOf(l);
                        Recognitions[index].Count++;
                        Recognitions[index].Photos.Add(q);
                    }
                    ImagesCounter++;
                });
            }
        }

//===========================================================================================//

        private BitmapImage ByteToImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new Exception("null");
            }

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
        }
        private byte[] ImagetoByte(Object bmpa)
        {
            var bmp = (BitmapImage)bmpa;
            byte[] data;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            try
            {
                encoder.Frames.Add(BitmapFrame.Create(bmp));
            }
            catch { }

            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = ms.ToArray();
            }
            return data;
        }
        private async Task<List<Photo>> GenerateImagesFromPathsAsync()
        {
            return await Task.Run(async () =>
            {
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
                    }
                    return a;
                });
                return await t;
            });

        }
        private async Task<List<Photo>> RemoveRecognizedImagesAsync()
        {
            return await Task.Run(() =>
            {
                for (int i = 0; i < Photos.Count; i++)
                {
                    var q = from rec in Recognitions
                            from ph in rec.Photos
                            where ph.Path == Photos[i].Path
                            select ph.Pixels;

                    if (q != null)
                    {
                        var a = Photos[i].Pixels;

                        foreach (byte[] pxls in q)
                        {
                            if (pxls.Length == a.Length)
                            {
                                for (int j = 0; j < a.Length; j++)
                                {
                                    if (pxls[j] != a[j])
                                    {
                                        break;
                                    }
                                    if (j == a.Length - 1)
                                    {
                                        Photos[i].Path = null;
                                    }
                                }
                            }
                        }
                    }
                }
                return (from a in Photos
                        where a.Path != null
                        select a).ToList();
            });
        }

//===========================================================================================//    
    }
}
