
using System.Collections.Generic;

namespace DataBaseSetup
{
    public class Recognition
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Count { get; set; }
        public List<Photo> Photo { get; set; }
    }

    public class Photo
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public Blob Pixels { get; set; }
    }

    public class Blob
    {
        public int Id { get; set; }
        public byte[] Pixels { get; set; }
    }
}
