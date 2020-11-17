
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

namespace ImageRecognizerViewModel
{
    public class ImageRecognizerVM : BaseViewModel
    {
        public List<FileBlank> Files = new List<FileBlank>();

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
            IsRunning = true;            
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
                        //db.Entry(q).Reference(a => a.Pixels).Load();
                        //byte [] photo = 
                        images[i] = null;
                    }
                }
            }

            images = (from a in images
                      where a != null
                      select a).ToArray();

            ImagesCount = images.Length;
            Trace.WriteLine(ImagesCount);
            ImagesCounter = 0;
            ImageRecognizer.onnxModelPath = OnnxModelPath;
          
            await ImageRecognizer.RecognitionAsync(images);

            using (var db = new Context())
            {
                foreach (var r in Files)
                {
                    List<DataBaseSetup.Photo> a = new List<DataBaseSetup.Photo>();
                    foreach (var photo in r.Photos)
                    {
                        a.Add(new DataBaseSetup.Photo
                        {
                            Path = photo.Path,
                            Pixels = new Blob
                            {
                                Pixels = photo.Pixels
                            }
                        });
                    }

                    db.Recognitions.Add(new Recognition
                    {
                        Title = r.Recognition,
                        Count = r.Count,
                        Photo = a
                    });
                }
                db.SaveChanges();
            }

            Files.Clear();
            IsRunning = false;
        }

        public async Task Stop()
        {
            IsStopping = true;           
            await ImageRecognizer.CancelRecognitionAsync();
            using (var db = new Context())
            {
                foreach (var r in Files)
                {
                    List<DataBaseSetup.Photo> a = new List<DataBaseSetup.Photo>();
                    foreach (var photo in r.Photos)
                    {
                        a.Add(new DataBaseSetup.Photo
                        {
                            Path = photo.Path,
                            Pixels = new Blob
                            {
                                Pixels = photo.Pixels
                            }
                        });
                    }

                    db.Recognitions.Add(new Recognition
                    {
                        Title = r.Recognition,
                        Count = r.Count,
                        Photo = a
                    });
                }
                db.SaveChanges();
            }
            Files.Clear();
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
    }

    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FileBlank
    {
        public string Recognition;
        public int Count;
        public List<Photo> Photos;

        public FileBlank(string s, string path, byte[] i)
        {
            Recognition = s;
            Count = 1;
            Photos = new List<Photo>();
            Photos.Add(new Photo
            {
                Path = path,
                Pixels = i
            });
        }
    }
    public struct Photo
    {
        public string Path;
        public byte[] Pixels;
    }
}
