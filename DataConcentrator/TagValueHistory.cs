using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    [Table("TagValueHistory")]
    public class TagValueHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [ForeignKey("Tag")]
        public string TagName { get; set; }

        [Required]
        public double Value { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public virtual Tag Tag { get; set; }
    }
}