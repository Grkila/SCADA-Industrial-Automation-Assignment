using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            try
            {
                Alarms.Add(new Alarm
                {
                    Id = Guid.NewGuid().ToString(),  // Simple GUID-based unique ID
                    TagName = alarm.TagName,
                    Type = alarm.Type,
                    Limit = alarm.Limit,
                    Message = alarm.Message
                });

                SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                // This is the crucial part to find the error
                foreach (var entityValidationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in entityValidationErrors.ValidationErrors)
                    {
                        // Use Console.WriteLine or Debug.WriteLine to see the error
                        System.Diagnostics.Debug.WriteLine(
                            $"Entity: {entityValidationErrors.Entry.Entity.GetType().Name}, Property: {validationError.PropertyName}, Error: {validationError.ErrorMessage}");
                    }
                }
                // Re-throw the exception if you want the application to still crash
                throw;
            }
        }

        public void DeleteAlarm(Alarm alarm)
        {
            Alarms.Remove(alarm);
            SaveChanges();
        }

    }
}
