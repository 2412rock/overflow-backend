namespace OverflowBackend.Models.DB
{
    public class DBFriend
    {
        public int FriendId { get; set; }
        public int UserId { get; set; }
        public int FriendUserId { get; set; }
        public FriendRequestStatus Status { get; set; }
    }
}
