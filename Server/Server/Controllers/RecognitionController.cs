
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
//using ImageRecognition;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RecognitionController : ControllerBase
    {
        private static IHubContext<Hubs.RecognitionHub> hubContext;

        public RecognitionController(IHubContext<Hubs.RecognitionHub> ahubContext)
        {
            hubContext = ahubContext;
        }

        public static async void RealTimeAdd(ImageRecognition.Prediction s)
        {
            await hubContext.Clients.All.SendAsync("RealTimeAdd", s.Label, s.Path);
        }

        [HttpGet("add")]
        public List<Recognition> Add()
        {
            Console.WriteLine("ADD");
            var a = new List<Recognition>();

            lock (Program.Recognitions)
            {
                foreach (var b in Program.Recognitions)
                {
                    var c = new Recognition
                    {
                        Count = b.Count,
                        Title = b.Title,
                        Photos = new ObservableCollection<Photo>()
                    };
                    foreach (var d in b.Photos)
                    {
                        c.Photos.Add(new Photo
                        {
                            Path = d.Path
                        });
                    }
                    a.Add(c);
                }
                Program.Recognitions.Clear();
            }
            Console.WriteLine("add");
            return a;
        }
        
        [HttpGet]
        public async Task<List<Recognition>> Test()
        {
            Console.WriteLine("LOAD");
            return await Task.Run(() =>
            {
                List<Recognition> b = new List<Recognition>();
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
                                Image = null
                            });
                        }
                        b.Add(new Recognition
                        {
                            Id = r.Id,
                            Title = r.Title,
                            Count = r.Photos.Count,
                            Photos = a
                        });
                    }
                }
                Console.WriteLine("load");
                return b;
            });
        }

        [HttpGet("{id}")]
        public async Task<IEnumerable<string>> Photos(int id)
        {
            Console.WriteLine($"AAA{id}AAA");

            using var db = new DataBaseSetup.Context();
            return await (from a in db.Photos
                          where a.RecognitionId == id
                          select Convert.ToBase64String(a.Pixels.Pixels)).ToListAsync();
        }

        [HttpGet("load")]
        public async Task<List<Recognition>> Load()
        {
            Console.WriteLine("LOAD");
            return await Task.Run(() =>
            {
                List<Recognition> b = new List<Recognition>();
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
                                PixelsString = Convert.ToBase64String(photo.Pixels.Pixels),
                                Pixels = null,
                                Image = null
                            });
                        }
                        b.Add(new Recognition
                        {
                            Title = r.Title,
                            Count = r.Photos.Count,
                            Photos = a
                        });
                    }
                }
                Console.WriteLine("load");
                return b;
            });
        }

        [HttpPost("start")]
        public async Task StartAsync(StartOptions rec)
        {
            Console.WriteLine($"START {rec.Images.Count}");

            foreach (var a in rec.Images) Console.WriteLine(a.Path);
            Console.WriteLine("=================");
            ImageRecognition.ImageRecognizer.onnxModelPath = rec.Onnx;
            Program.Photos = rec.Images;
            if (Program.flag)
            {
                Program.flag = false;
                Console.WriteLine("start");
                return;
            }
            await ImageRecognition.ImageRecognizer.RecognitionAsync(from i in rec.Images
                                                   select i.Path);
            Program.flag = false;
            Console.WriteLine("start");
        }

        [HttpPost("stop")]
        public async Task StopAsync()
        {
            Console.WriteLine("STOP");
            Program.flag = true;
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
            Program.Recognitions.Clear();
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
