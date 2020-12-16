using Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Server.DataBases
{
    public class MyDB : Interfaces.IDataBase
    {
        public async Task<IEnumerable<Recognition>> GetAllRecognitions()
        {
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
        public async Task<IEnumerable<Recognition>> GetAllRecognitionsWithoutImages()
        {
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
                return b;
            });
        }
        public async Task<IEnumerable<string>> GetPhotosFromRecognitionWithId(int id)
        {
            return await Task.Run(() =>
            {
                var db = new DataBaseSetup.Context();
                return from a in db.Photos
                       where a.RecognitionId == id
                       select Convert.ToBase64String(a.Pixels.Pixels);
            });
        }
        public async Task Save(IEnumerable<Recognition> recognitons)
        {
            await Task.Run(async () =>
            {
                using (var db = new DataBaseSetup.Context())
                {
                    foreach (var r in recognitons)
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
        public async Task Clear()
        {
            await Task.Run(async () =>
            {
                using (var db = new DataBaseSetup.Context())
                {
                    db.Recognitions.RemoveRange(db.Recognitions);
                    db.Photos.RemoveRange(db.Photos);
                    db.Blobs.RemoveRange(db.Blobs);
                    await db.SaveChangesAsync();
                }
            });
        }
    }
}
