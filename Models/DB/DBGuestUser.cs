using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OverflowBackend.Models.DB
{
    public class DBGuestUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        [Required]
        public string Username { get; set; }
        public int NumberOfGames { get; set; }
    }
}
