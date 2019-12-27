using Microsoft.EntityFrameworkCore;
using PicColl.DBContext.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PicColl.DBContext
{
    public class PicDBContext : DbContext
    {
        public DbSet<PicInfo> PicInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=picdb.db");
        }
    }
}
