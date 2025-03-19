using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OverflowBackend.Models.DB
{
    public class DBOwnedSkins
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SkinId { get; set; }

        public DBUser User { get; set; } = null!; // Ensure User navigation property is present

        public DBSkin Skin { get; set; } = null!;
    }
}
