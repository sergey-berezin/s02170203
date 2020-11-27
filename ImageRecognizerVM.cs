
using System;
using System.IO;
using ImageRecognition;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media.Imaging;

using Microsoft.EntityFrameworkCore;
using DataBaseSetup;

namespace WPF
{
    public class ImageRecognizerVM : BaseViewModel
    {
        public ObservableCollection<Recognition> Recognitions { get; set; }
        public List<Photo> Photos { get; set; }

//===========================================================================================//

        public ImageRecognizerVM()
        {
            ImageRecognizer.Result += Add;

            Recognitions = Load();
            Photos = new List<Photo>();
        }

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
                return !IsStopping;
            }
        }
        public bool ClearButtonEnabled
        {
            get
            {
                return !IsClearing;
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

        public bool isClearing = false;
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

//===========================================================================================// 
        
        public async Task StartAsync()
        {
            IsRunning = true;
            Photos = await ImageGeneratorAsync(ImagesPath);
            string[] images = await RemoveRecognizedImagesAsync();

            ImagesCount = images.Length;
            ImagesCounter = 0;
            ImageRecognizer.onnxModelPath = OnnxModelPath;

            await ImageRecognizer.RecognitionAsync(images);

            await SaveAsync();
            Photos.Clear();
            IsRunning = false;
        }
        public async Task StopAsync()
        {
            IsStopping = true;

            await ImageRecognizer.CancelRecognitionAsync();
            await SaveAsync();

            Photos.Clear();
            IsStopping = false;
        }
        public async Task ClearAsync()
        {
            IsClearing = true;
            Recognitions.Clear();
            Photos.Clear();
            using (var db = new Context())
            {
                db.Recognitions.RemoveRange(db.Recognitions);
                db.Photos.RemoveRange(db.Photos);
                db.Blobs.RemoveRange(db.Blobs);
                await db.SaveChangesAsync();
            }
            IsClearing = false;
        }

//===========================================================================================//
       
        private void Add(Prediction prediction)
        {
            string path = ImagesPath + "\\" + prediction.Path;

            App.Current.Dispatcher.Invoke(() =>
            {
                var l = (from pic in Recognitions
                         where pic.Title == prediction.Label
                         select pic).FirstOrDefault();

                var q = (from pic in Photos
                         where path == pic.Path
                         select pic).FirstOrDefault();

                if (l == null) //first time 
                {
                    Recognitions.Add(new Recognition
                    {
                        Title = prediction.Label,
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

//===========================================================================================//       
        
        private async Task SaveAsync()
        {
            await Task.Run(async () =>
            {
                using (var db = new Context())
                {
                    foreach (var r in Recognitions)
                    {
                        var rec = (from b in db.Recognitions
                                   where r.Title == b.Title
                                   select b).FirstOrDefault();

                        if (rec == null)
                        {
                            db.Recognitions.Add(new DataBaseSetup.Recognition
                            {
                                Title = r.Title,
                            });
                            await db.SaveChangesAsync();

                            rec = (from b in db.Recognitions
                                   where b.Title == r.Title
                                   select b).FirstOrDefault();
                        }

                        List<DataBaseSetup.Photo> a = new List<DataBaseSetup.Photo>();
                        foreach (var photo in r.Photos)
                        {
                            if (!photo.IsSavedInDataBase)
                            {
                                a.Add(new DataBaseSetup.Photo
                                {
                                    Path = photo.Path,
                                    Pixels = new Blob
                                    {
                                        Pixels = photo.Pixels
                                    },
                                    RecognitionId = rec.Id
                                });
                            }
                            photo.IsSavedInDataBase = true;
                        }
                        db.Photos.AddRange(a);
                    }
                    await db.SaveChangesAsync();
                }
            });
        }
        private async Task<string[]> RemoveRecognizedImagesAsync()
        {
            return await Task.Run(() =>
            {
                string[] images = Directory.GetFiles(ImagesPath);

                using (var db = new Context())
                {
                    for (int i = 0; i < images.Length; i++)
                    {
                        var q = (from a in db.Photos
                                 where a.Path == images[i]
                                 select a).FirstOrDefault();

                        if (q != null)
                        {
                            db.Entry(q).Reference(a => a.Pixels).Load();

                            var a = (from b in Photos
                                     where q.Path == b.Path
                                     select b.Pixels).FirstOrDefault();

                            if (q.Pixels.Pixels.Length == a.Length)
                            {
                                for (int j = 0; j < a.Length; j++)
                                {
                                    if (q.Pixels.Pixels[j] != a[j])
                                    {
                                        break;
                                    }
                                    if (j == a.Length - 1)
                                    {
                                        images[i] = null;
                                    }
                                }
                            }
                        }
                    }
                }

                return images = (from a in images
                                 where a != null
                                 select a).ToArray();
            });
        }
        private ObservableCollection<Recognition> Load()
        {
            //return await Task<List<Recognition>>.Run(() =>
            //{
            ObservableCollection<Recognition> b = new ObservableCollection<Recognition>();
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
                    b.Add(new Recognition
                    {
                        Title = r.Title,
                        Count = r.Photos.Count,
                        Photos = a
                    });
                }
            }
            return b;
            //});
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
        private byte[] ImagetoByte(BitmapImage bmp)
        {
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
        private async Task<List<Photo>> ImageGeneratorAsync(string path)
        {
            return await Task.Run(async () =>
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
            });

        }

//===========================================================================================//    
    }
}
