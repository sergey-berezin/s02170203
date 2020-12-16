using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Interfaces
{
    public interface IDataBase
    {
        public Task<IEnumerable<Contracts.Recognition>> GetAllRecognitionsWithoutImages();
        public Task<IEnumerable<Contracts.Recognition>> GetAllRecognitions();
        public Task<IEnumerable<string>> GetPhotosFromRecognitionWithId(int id);
        public Task Save(IEnumerable<Contracts.Recognition> recognitons);
        public Task Clear();
    }
}
