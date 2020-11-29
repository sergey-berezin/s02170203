
using System;

namespace Contracts
{
    public class Photo
    {
        public bool IsSavedInDataBase { get; set; } = false;
        public string Path { get; set; }
        public byte[] Pixels { get; set; } = null;
        public Object Image { get; set; }
    }
}
