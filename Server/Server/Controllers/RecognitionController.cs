
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Contracts;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RecognitionController : ControllerBase
    {
        [HttpGet("add")]
        public List<Recognition> Add()
        {
            Console.WriteLine("ADD");
            var a = new List<Recognition>();

            lock (Program.NewRecognitions)
            {
                foreach (var b in Program.NewRecognitions)
                {
                    var c = new Recognition
                    {
                        Count = b.Count,
                        Title = b.Title,
                        Photos = new ObservableCollection<Photo>()
                    };
                    foreach(var d in b.Photos)
                    {
                        c.Photos.Add(new Photo
                        {
                            Path = d.Path
                        });
                    }
                    a.Add(c);
                }
                Program.NewRecognitions.Clear();
            }
            Console.WriteLine("add");
            return a;
        }

        [HttpGet("load")]
        public List<Recognition> Load()
        {
            return Program.Recognitions;
        }

        [HttpPost("start")]
        public async Task<List<Recognition>> StartAsync(StartOptions rec)
        {
            Console.WriteLine($"START {rec.Images.Count}");

            ImageRecognition.ImageRecognizer.onnxModelPath = rec.Onnx;
            Program.Photos = rec.Images;
            await ImageRecognition.ImageRecognizer.RecognitionAsync(from i in rec.Images
                                                   select i.Path);
            
            Console.WriteLine("start");
            return Program.Recognitions;
        }

        [HttpPost("stop")]
        public async Task Stop()
        {
            Console.WriteLine("STOP");
            await ImageRecognition.ImageRecognizer.CancelRecognitionAsync();
            Console.WriteLine("stop");
        }

        [HttpPut("save")]
        public async Task SaveAsync()
        {
            Console.WriteLine("SAVE");
            await Task.Run(async () =>
            {
                using (var db = new DataBaseSetup.Context())
                {
                    foreach (var r in Program.Recognitions)
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
                                    Pixels = new DataBaseSetup.Blob
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
            Program.Photos.Clear();
            Console.WriteLine("save");
        }
    
        [HttpPut("clear")]
        public async Task ClearAsync()
        {
            Console.WriteLine("CLEAR");
            using (var db = new DataBaseSetup.Context())
            {               
                db.Recognitions.RemoveRange(db.Recognitions);
                db.Photos.RemoveRange(db.Photos);
                db.Blobs.RemoveRange(db.Blobs);
                await db.SaveChangesAsync();               
            }
            Program.Photos.Clear();
            Program.Recognitions.Clear();
            Console.WriteLine("clear");
        }
    }
}
