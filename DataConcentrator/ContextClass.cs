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

        public virtual DbSet<ActiveAlarm> ActivatedAlarms { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<Tag>()
                .HasMany(t => t.Alarms)
                .WithRequired(a => a.Tag)
                .HasForeignKey(a => a.TagName)
                .WillCascadeOnDelete(true);

            // Configure the CharacteristicsJson column for storing Dictionary as JSON
            modelBuilder.Entity<Tag>()
                .Property(t => t.CharacteristicsJson)
                .HasColumnType("NVARCHAR(MAX)")
                .IsOptional();

            base.OnModelCreating(modelBuilder);
        }
        public IEnumerable<Tag> GetTags() => Tags.ToList();
        public IEnumerable<Alarm> GetAlarms() => Alarms.ToList();
        public void AddTag(Tag tagFromViewModel)
        {
            var newTag = new Tag
            {
                Name = tagFromViewModel.Name,
                Type = tagFromViewModel.Type,
                IOAddress = tagFromViewModel.IOAddress,
                Description = tagFromViewModel.Description,
                CurrentValue = tagFromViewModel.InitialValue ?? 0 // Osiguravamo da i novi tagovi imaju početnu vrednost
            };
            foreach (var characteristic in tagFromViewModel.Characteristics)
            {
                newTag.Characteristics.Add(characteristic.Key, characteristic.Value);
            }
            Tags.Add(newTag);
            SaveChanges();
        }

        public void DeleteTag(Tag tag)
        {
            Tags.Remove(tag);
            SaveChanges();
        }

        public void AddAlarm(Alarm alarm)
        {
            Alarms.Add(new Alarm { TagName = alarm.TagName, Type = alarm.Type, Limit = alarm.Limit, Message = alarm.Message });
            SaveChanges();
        }

        public void DeleteAlarm(Alarm alarm)
        {
            Alarms.Remove(alarm);
            SaveChanges();
        }

    }
}
