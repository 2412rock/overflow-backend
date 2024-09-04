using OverflowBackend.Enums;

namespace OverflowBackend.Models.Response
{
    public class Friend
    {
        public string Username { get; set; }

        public int UserID { get; set; }

        public int Score {get; set; }

        public FriendOnlineStatus Status { get; set; }
    }
}
