using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts
{
    public class StartOptions
    {
        public string Onnx { get; set; }
        public List<Photo> Images { get; set; }
    }
}
