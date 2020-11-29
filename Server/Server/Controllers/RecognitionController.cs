
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Contracts;
using System.Diagnostics;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RecognitionController : ControllerBase
    {
        [HttpGet]
        public async Task<IEnumerable<Recognition>> LoadAsync()
        {
            return await Task.Run(() =>
            {
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
                                Pixels = photo.Pixels.Pixels,
                                Image = null
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
            });
        }
        
        [HttpPut]
        public async Task SaveAsync(IEnumerable<Recognition> recognitions)
        {
            await Task.Run(async () =>
            {
                using (var db = new DataBaseSetup.Context())
                {
                    foreach (var r in recognitions)
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
        }
    
        [HttpPost("start")]
        public async Task StartAsync(StartOptions startOptions)
        {
            Trace.WriteLine(startOptions.Onnx);
            Console.WriteLine(startOptions.Onnx);
        }
    
    }
}
