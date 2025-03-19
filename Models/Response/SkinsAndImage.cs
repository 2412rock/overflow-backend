using OverflowBackend.Models.DB;

namespace OverflowBackend.Models.Response
{
    public class SkinAndImage
    {
        public DBSkin Skin { get; set; }
        public Image Image { get; set; }
        public bool Owned { get; set; }
    }
}
