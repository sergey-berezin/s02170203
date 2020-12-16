
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using Server.Interfaces;
using Server.Hubs;
//using ImageRecognition;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RecognitionController : ControllerBase
    {
        private static IHubContext<RecognitionHub> hubContext;
        private IDataBase dataBase;
        
        public RecognitionController(IHubContext<RecognitionHub> ahubContext, IDataBase dataBase)
        {
            hubContext = ahubContext;
            this.dataBase = dataBase;
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
        
        [HttpGet("loadwithoutimages")]
        public async Task<List<Recognition>> LoadWithOutImages()
        {
            Console.WriteLine("LOAD");
            return (await dataBase.GetAllRecognitionsWithoutImages()).ToList(); ;
        }

        [HttpGet("{id}")]
        public async Task<IEnumerable<string>> Photos(int id)
        {
            Console.WriteLine($"AAA{id}AAA");

            return await dataBase.GetPhotosFromRecognitionWithId(id);
        }

        [HttpGet("load")]
        public async Task<List<Recognition>> Load()
        {
            Console.WriteLine("LOAD");
            return (await dataBase.GetAllRecognitions()).ToList();
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
            await dataBase.Save(Program.Recognitions);
            Program.Recognitions.Clear();
            Program.Photos.Clear();
            Console.WriteLine("save");
        }
    
        [HttpPut("clear")]
        public async Task ClearAsync()
        {
            Console.WriteLine("CLEAR");
            await dataBase.Clear();
            Program.Photos.Clear();
            Program.Recognitions.Clear();
            Console.WriteLine("clear");
        }
    }
}
