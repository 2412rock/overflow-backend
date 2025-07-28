using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OverflowBackend.Models.DB
{
    public class DBOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }
        public string TransactionId { get; set; }
        public bool Finalized { get; set; }
    }
}
