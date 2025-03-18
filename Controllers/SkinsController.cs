using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OverflowBackend.Enums;
using OverflowBackend.Filters;
using OverflowBackend.Models.Requests;
using OverflowBackend.Models.Response;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services;
using OverflowBackend.Services.Implementantion;
using OverflowBackend.Services.Interface;

namespace OverflowBackend.Controllers
{
    [Route("api")]
    [ApiController]
    public class SkinsController : ControllerBase
    {
        private readonly BlobStorageService _blob;
        private readonly OverflowDbContext _context;
        public SkinsController(BlobStorageService blobStorageService, OverflowDbContext context)
        {
            _blob = blobStorageService;
            _context = context;
        }

        [HttpPost]
        [Route("addSkin")]
        [AuthorizationFilter]
        public async Task<IActionResult> AddSkin([FromBody] Image image)
        {
            try
            {
                await _context.Skins.AddAsync(new Models.DB.DBSkin() { SkinName = image.FileName, PriceShopPoints = 350 });
                await _blob.UploadImage(image.FileName, image.FileType, image.FileContentBase64, "skins");
                await _context.SaveChangesAsync();
            }
            catch
            {
                _context.ChangeTracker.Clear();
                throw;
            }
            return Ok("success");
        }

        [HttpGet]
        [Route("getSkins")]
        [AuthorizationFilter]
        public async Task<IActionResult> GetSkins()
        {
            var maybe = new Maybe<List<SkinAndImage>>();
            try
            {
                var skinsAndImages = new List<SkinAndImage>();
                var skins = await _context.Skins.ToListAsync();
                var username = (string)HttpContext.Items["username"];
                var user = await _context.Users.FirstOrDefaultAsync(e => e.Username == username);
                foreach (var skin in skins)
                {
                    var any = await _context.OwnedSkins.AnyAsync(e => e.SkinId == skin.Id && e.UserId == user.UserId);
                    var obj = new SkinAndImage()
                    {
                        Image = await _blob.DownloadImage(skin.SkinName, "skins"),
                        Skin = skin,
                        Owned = any
                    };
                    skinsAndImages.Add(obj);
                }
                maybe.SetSuccess(skinsAndImages);
            }
            catch(Exception e)
            {
                maybe.SetException(e.Message);
            }
            
            return Ok(maybe);
        }

        [HttpGet]
        [Route("getOwnedSkins")]
        [AuthorizationFilter]
        public async Task<IActionResult> GetOwnedSkins()
        {
            var maybe = new Maybe<List<SkinAndImage>>();
            try
            {
                var skinsAndImages = new List<SkinAndImage>();
                var skins = await _context.Skins.ToListAsync();
                var username = (string)HttpContext.Items["username"];
                var user = await _context.Users.FirstOrDefaultAsync(e => e.Username == username);
                foreach (var skin in skins)
                {
                    var any = await _context.OwnedSkins.AnyAsync(e => e.SkinId == skin.Id && e.UserId == user.UserId);
                    if (any)
                    {
                        var obj = new SkinAndImage()
                        {
                            Image = await _blob.DownloadImage(skin.SkinName, "skins"),
                            Skin = skin,
                            Owned = any
                        };
                        skinsAndImages.Add(obj);
                    }
                    
                }
                maybe.SetSuccess(skinsAndImages);
            }
            catch (Exception e)
            {
                maybe.SetException(e.Message);
            }

            return Ok(maybe);
        }

        [Route("shopPoints")]
        [AuthorizationFilter]
        [HttpGet]
        public async Task<IActionResult> GetUserShopPoints()
        {
            var maybe = new Maybe<int>();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(e => e.Username == (string)HttpContext.Items["username"]);
                maybe.SetSuccess(user.ShopPoints);
            }
            catch(Exception e)
            {
                maybe.SetException(e.Message);
            }
            return Ok(maybe);
        }

        [Route("purchaseskin")]
        [AuthorizationFilter]
        [HttpPost]
        public async Task<IActionResult> PurchaseSkin([FromBody] PurchaseSkinRequest request)
        {
            var maybe = new Maybe<string>();
            try
            {
                var username = (string)HttpContext.Items["username"];
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                var skin = await _context.Skins.FirstOrDefaultAsync(e => e.Id == request.SkinId);
                if(user.ShopPoints > skin.PriceShopPoints)
                {
                    await _context.OwnedSkins.AddAsync(new Models.DB.DBOwnedSkins()
                    {
                        SkinId = request.SkinId,
                        UserId = user.UserId
                    });
                    user.ShopPoints -= skin.PriceShopPoints;
                    await _context.SaveChangesAsync();
                    maybe.SetSuccess("Purchase success");
                }
                else
                {
                    maybe.SetException("Insufficient funds");
                }
                
            }
            catch(Exception e)
            {
                maybe.SetException(e.Message);
            }
            return Ok(maybe);
        }

        [Route("useSkin")]
        [AuthorizationFilter]
        [HttpPut]
        public async Task<IActionResult> UseSkin(PurchaseSkinRequest request)
        {
            var maybe = new Maybe<string>();
            try
            {
                var skinsAndImages = new List<SkinAndImage>();
                var username = (string)HttpContext.Items["username"];
                var user = await _context.Users.FirstOrDefaultAsync(e => e.Username == username);

                var any = await _context.OwnedSkins.AnyAsync(e => e.SkinId == request.SkinId && e.UserId == user.UserId);
                if (any)
                {
                    user.CurrentSkinId = request.SkinId;
                    await _context.SaveChangesAsync();
                    maybe.SetSuccess("ok");
                }
                else
                {
                    maybe.SetException("skin not owned");
                }
            }
            catch (Exception e)
            {
                maybe.SetException(e.Message);
            }

            return Ok(maybe);
        }

        [Route("getUserSkin")]
        [AuthorizationFilter]
        [HttpGet]
        public async Task<IActionResult> GetUserSkin([FromQuery] string username)
        {
            var maybe = new Maybe<SkinAndImage>();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(e => e.Username == username);
                var skin = await _context.Skins.FirstOrDefaultAsync(e => e.Id == user.CurrentSkinId);
                if (skin == null)
                {
                    maybe.SetSuccess(null);
                }
                else
                {
                    var image = await _blob.DownloadImage(skin.SkinName, "skins");
                    var obj = new SkinAndImage()
                    {
                        Image = image,
                        Skin = skin,
                        Owned = true
                    };
                    maybe.SetSuccess(obj);
                }
            }
            catch (Exception e)
            {
                maybe.SetException(e.Message);
            }

            return Ok(maybe);
        }

        [Route("getMyCurrentSkin")]
        [AuthorizationFilter]
        [HttpGet]
        public async Task<IActionResult> GetMyCurrentSkin()
        {
            var maybe = new Maybe<SkinAndImage>();
            try
            {
                var username = (string)HttpContext.Items["username"];
                var user = await _context.Users.FirstOrDefaultAsync(e => e.Username == username);
                var skin = await _context.Skins.FirstOrDefaultAsync(e => e.Id == user.CurrentSkinId);
                if(skin == null)
                {
                    maybe.SetSuccess(null);
                }
                else
                {
                    var image = await _blob.DownloadImage(skin.SkinName, "skins");
                    var obj = new SkinAndImage()
                    {
                        Image = image,
                        Skin = skin,
                        Owned = true
                    };
                    maybe.SetSuccess(obj);
                }
            }
            catch(Exception e)
            {
                maybe.SetException(e.Message);
            }

            return Ok(maybe);
        }
    }
}
