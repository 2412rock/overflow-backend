using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using OverflowBackend.Enums;

namespace OverflowBackend.Models.DB
{
    public class DBFriend
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FriendId { get; set; }
        public int UserId { get; set; }
        public int FriendUserId { get; set; }
        public FriendRequestStatus Status { get; set; }
    }
}
