using Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        public static List<Recognition> Recognitions { get; set; } = new List<Recognition>();
        public static List<Photo> Photos { get; set; } = new List<Photo>();
        //public static List<Recognition> NewRecognitions { get; set; } = new List<Recognition>();
        public static async Task Main(string[] args)
        {
            ImageRecognition.ImageRecognizer.Result += Add;
            Recognitions = await LoadAsync();

            CreateHostBuilder(args).Build().Run();            
        }

        private static void Add(ImageRecognition.Prediction prediction)
        {
            Server.Controllers.RecognitionController.RealTimeAdd(prediction);
        }
        //    lock (Recognitions)
        //    { 
        //        var l = (from pic in Recognitions
        //                 where pic.Title == prediction.Label
        //                 select pic).FirstOrDefault();

        //        var q = (from pic in Photos
        //                 where prediction.Path == pic.Path
        //                 select pic).FirstOrDefault();

        //        if (l == null) //first time 
        //        {
        //            Recognitions.Add(new Recognition
        //            {
        //                Title = prediction.Label,
        //                Count = 1,
        //                Photos = new ObservableCollection<Photo> { q }
        //            });
        //        }
        //        else
        //        {
        //            int index = Recognitions.IndexOf(l);
        //            Recognitions[index].Count++;
        //            Recognitions[index].Photos.Add(q);
        //        }           
        //    }
        //    lock (NewRecognitions)
        //    {
        //        var l = (from pic in NewRecognitions
        //                 where pic.Title == prediction.Label
        //                 select pic).FirstOrDefault();

        //        var q = (from pic in Photos
        //                 where prediction.Path == pic.Path
        //                 select pic).FirstOrDefault();

        //        if (l == null) //first time 
        //        {
        //            NewRecognitions.Add(new Recognition
        //            {
        //                Title = prediction.Label,
        //                Count = 1,
        //                Photos = new ObservableCollection<Photo> { q }
        //            });
        //        }
        //        else
        //        {
        //            int index = NewRecognitions.IndexOf(l);
        //            NewRecognitions[index].Count++;
        //            NewRecognitions[index].Photos.Add(q);
        //        }
        //    }
        //}

        public static async Task<List<Recognition>> LoadAsync()
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
                                Pixels = photo.Pixels.Pixels,
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
        
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
