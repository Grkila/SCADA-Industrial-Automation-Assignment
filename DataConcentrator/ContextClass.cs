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
        public virtual DbSet<TagValueHistory> TagValueHistory { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<Tag>()
                .HasMany(t => t.Alarms)
                .WithRequired(a => a.Tag)
                .HasForeignKey(a => a.TagName)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Tag>()
                .Property(t => t.CharacteristicsJson)
                .HasColumnType("NVARCHAR(MAX)")
                .IsOptional();

            base.OnModelCreating(modelBuilder);
        }
        public IEnumerable<Tag> GetTags() => Tags.ToList();
        public IEnumerable<Alarm> GetAlarms() => Alarms.ToList();
        public void AddTag(Tag tag)
        {
            Tags.Add(tag);
            if (string.IsNullOrEmpty(tag.Description))
            {
                tag.Description = $"{tag.Name} with address {tag.IOAddress}";
            }
            SaveChanges();
        }
        public void DeleteTag(Tag tag)
        {
            var tagToDelete = Tags.Find(tag.Name);

            if (tagToDelete != null)
            {
                Tags.Remove(tagToDelete);
                SaveChanges();
            }
        }

        public void AddAlarm(Alarm alarm)
        {
            try
            {
                Alarms.Add(alarm);
                SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var entityValidationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in entityValidationErrors.ValidationErrors)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Entity: {entityValidationErrors.Entry.Entity.GetType().Name}, Property: {validationError.PropertyName}, Error: {validationError.ErrorMessage}");
                    }
                }
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
