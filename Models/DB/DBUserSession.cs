using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OverflowBackend.Models.DB
{
    public class DBUserSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SessionId { get; set; }
        public string Username { get; set; }
        public string SessionToken { get; set; }
        public DateTime LastActiveTime { get; set; }
        public bool IsActive { get; set; }
    }
}
