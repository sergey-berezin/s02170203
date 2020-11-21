
using System;
using System.ComponentModel;
using System.IO;
using ImageRecognition;
using System.Threading.Tasks;
using DataBaseSetup;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

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

        public Recognition(string s, string path, Object i)
        {
            Title = s;
            Photos = new ObservableCollection<Photo>();

            Photos.Add(new Photo
            {
                Path = path,
                Image = i
            });
            count = 1;
        }

        public Recognition() { }

        public override string ToString()
        {
            string s = "";
            s += Title;
            s += "  ";
            s += Count.ToString() + "  ";
            foreach (var a in Photos)
            {
                s += a.Path + "\n";
            }
            return s;
        }

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

        public string ControlButtonContent { get; private set; } = "Start";
        public bool ControlButtonEnabled { get; private set; } = true;

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
            //IsRunning = true;            
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

                        if(q.Pixels.Pixels.Length == a.Length)
                        {
                            for(int j = 0; j < a.Length; j++)
                            {
                                if(q.Pixels.Pixels[j] != a[j])
                                {
                                    break;
                                }
                                if(j == a.Length - 1)
                                {
                                    images[i] = null;
                                    Trace.WriteLine("BBB");
                                }
                            }
                        }
                    }
                }
            }

            images = (from a in images
                      where a != null
                      select a).ToArray();

            ImagesCount = images.Length;
            ImagesCounter = 0;
            ImageRecognizer.onnxModelPath = OnnxModelPath;
            await ImageRecognizer.RecognitionAsync(images);

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
                        db.SaveChanges();

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
                db.SaveChanges();
            }
            IsRunning = false;
        }

        public async Task Stop()
        {
            IsStopping = true;           
            await ImageRecognizer.CancelRecognitionAsync();
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
                        db.SaveChanges();

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
                db.SaveChanges();
            }
            IsStopping = false;
        }

        public async Task Clear()
        {
            using (var db = new Context())
            {
                db.Recognitions.RemoveRange(db.Recognitions);
                db.Photos.RemoveRange(db.Photos);
                db.Blobs.RemoveRange(db.Blobs);
                await db.SaveChangesAsync();
            }
        }

//===========================================================================================//


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
