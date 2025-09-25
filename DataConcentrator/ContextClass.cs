using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Threading;

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

            // Configure the CharacteristicsJson column for storing Dictionary as JSON
            modelBuilder.Entity<Tag>()
                .Property(t => t.CharacteristicsJson)
                .HasColumnType("NVARCHAR(MAX)")
                .IsOptional();

            base.OnModelCreating(modelBuilder);
        }
    }
    
}
