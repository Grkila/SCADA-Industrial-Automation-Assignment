using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace DataConcentrator
{
    public class ContextClass : DbContext
    {
        public ContextClass() : base("ScadaConnectionString")
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<ContextClass>());
        }

        public virtual DbSet<Tag> Tags { get; set; }
        public virtual DbSet<Alarm> Alarms { get; set; }
        public virtual DbSet<ActivatedAlarm> ActivatedAlarms { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<Tag>()
                .HasMany(t => t.Alarms)
                .WithRequired(a => a.Tag)
                .HasForeignKey(a => a.TagId)
                .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }
    }
}
