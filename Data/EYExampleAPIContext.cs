using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EYExampleAPI.Models
{
    public class EYExampleAPIContext : DbContext
    {
        public EYExampleAPIContext (DbContextOptions<EYExampleAPIContext> options)
            : base(options)
        {
        }

        public DbSet<EYExampleAPI.Models.ExampleItem> ExampleItem { get; set; }
    }
}
