using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OverflowBackend.Models.DB
{
    public class DBSkin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string SkinName { get; set; }
        public int PriceShopPoints { get; set; }
    }
}
