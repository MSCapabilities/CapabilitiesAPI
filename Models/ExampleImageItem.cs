using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EYExampleAPI.Models
{
    public class ExampleImageItem
    {
        public string Title { get; set; }
        public string Tags { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile Image { get; set; }
    }
}
