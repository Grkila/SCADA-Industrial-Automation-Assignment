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
        public ContextClass() : base("ScadaConnection")
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

        public IEnumerable<Tag> GetTags() => Tags.ToList();

        public IEnumerable<Alarm> GetAlarms() => Alarms.ToList();

        public void AddTag(Tag tagFromViewModel)
        {
            var newTag = new Tag(tagFromViewModel.Type, tagFromViewModel.Id, tagFromViewModel.Description, tagFromViewModel.IOAddress)
            {
                CurrentValue = tagFromViewModel.InitialValue ?? 0 // Osiguravamo da i novi tagovi imaju početnu vrednost
            };

            // Copy characteristics if they exist
            if (tagFromViewModel._characteristics != null)
            {
                foreach (var characteristic in tagFromViewModel._characteristics)
                {
                    newTag.SetCharacteristic(characteristic.Key, characteristic.Value);
                }
            }

            Tags.Add(newTag);
        }

        public void DeleteTag(Tag tag)
        {
            var existingTag = Tags.Find(tag.Id);
            if (existingTag != null)
            {
                Tags.Remove(existingTag);
            }
        }

        public void AddAlarm(Alarm alarm)
        {
            var newAlarm = new Alarm(alarm.Id, alarm.Trigger, alarm.Threshold, alarm.Message)
            {
                TagId = alarm.TagId
            };
            Alarms.Add(newAlarm);
        }

        public void DeleteAlarm(Alarm alarm)
        {
            var existingAlarm = Alarms.Find(alarm.Id);
            if (existingAlarm != null)
            {
                Alarms.Remove(existingAlarm);
            }
        }
    }

}