
using System;
using System.ComponentModel;
using System.IO;
using ImageRecognition;
using System.Threading.Tasks;
using DataBaseSetup;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace ImageRecognizerViewModel
{
    public class Recognition : BaseViewModel
    {
        private string title;
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                OnPropertyChanged(nameof(Title));
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

        public Recognition() { }

    }
    public class Photo
    {
        public bool IsSavedInDataBase { get; set; } = false;
        public string Path { get; set; }
        public byte[] Pixels { get; set; } = null;
        public object Image { get; set; }
    }
    
    public class ImageRecognizerVM : BaseViewModel
    {
        public ObservableCollection<Recognition> Recognitions { get; set; }

        public List<Photo> Photos { get; set; }

        public ImageRecognizerVM()
        {
            Recognitions = new ObservableCollection<Recognition>();
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

        public async Task Start()
        {
            //IsRunning = true;            
            string[] images = await RemoveRecognizedImages();

            ImagesCount = images.Length;
            ImagesCounter = 0;
            ImageRecognizer.onnxModelPath = OnnxModelPath;

            await ImageRecognizer.RecognitionAsync(images);

            await Save();
            Photos.Clear();
            IsRunning = false;
        }

        public async Task Stop()
        {
            IsStopping = true;           
            
            await ImageRecognizer.CancelRecognitionAsync();
            await Save();

            Photos.Clear();
            IsStopping = false;
        }

        public async Task Clear()
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

        private async Task Save()
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
    
        private async Task<string[]> RemoveRecognizedImages()
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
