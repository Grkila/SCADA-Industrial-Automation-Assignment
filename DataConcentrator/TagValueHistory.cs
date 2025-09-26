using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    [Table("TagValueHistory")]
    public class TagValueHistory
    {
        [Key]
        public int Id { get; set; } // Primarni ključ za svaki zapis

        [Required]
        [StringLength(50)] // Mora da se poklapa sa dužinom imena taga
        [ForeignKey("Tag")]
        public string TagName { get; set; } // Spoljni ključ ka tabeli Tags

        [Required]
        public double Value { get; set; } // Vrednost koja je očitana

        [Required]
        public DateTime Timestamp { get; set; } // Vreme kada je vrednost očitana

        // Navigaciono svojstvo ka Tagu
        public virtual Tag Tag { get; set; }
    }
}