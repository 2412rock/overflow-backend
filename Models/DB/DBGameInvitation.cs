namespace OverflowBackend.Models.DB
{
    public class DBGameInvitation
    {
        public int InvitationId { get; set; }
        public int SenderUserId { get; set; }
        public int ReceiverUserId { get; set; }
        public RequestStatus Status { get; set; } // Use the enum for status
        public DateTime CreatedAt { get; set; }
    }
}
