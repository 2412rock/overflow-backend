using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OverflowBackend.Models.DB
{
    public class DBUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        [Required]
        public string Username { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; }
        public int Rank { get; set; }

        public int NumberOfGames { get; set; }

    }
}
