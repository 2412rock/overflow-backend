using Microsoft.EntityFrameworkCore;

namespace OverflowBackend.Services.Implementantion
{
    public class CoinsService
    {
        private readonly OverflowDbContext _context;
        private static readonly int CoinsPerWonGame = 30;
        public CoinsService(OverflowDbContext context) 
        {
            _context = context;
        }

        public async Task GrantCoinsEndGame(string username, bool win)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
            if (user != null) 
            {
                if (win)
                {
                    user.ShopPoints += CoinsPerWonGame;
                }
                else
                {
                    user.ShopPoints -= CoinsPerWonGame;
                }
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}
