using Google;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OverflowBackend.Models.DB;
using OverflowBackend.Models.Response;
using OverflowBackend.Models.Response.DorelAppBackend.Models.Responses;
using OverflowBackend.Services.Interface;
using IConnectionManager = OverflowBackend.Services.Interface.IConnectionManager;

namespace OverflowBackend.Services.Implementantion
{
    public class FriendService : IFriendService
    {
        private readonly OverflowDbContext _dbContext;
        IConnectionManager _connectionManager;

        public FriendService(OverflowDbContext dbContext, IConnectionManager connectionManager)
        {
            _dbContext = dbContext;
            _connectionManager = connectionManager;
        }

        public async Task<Maybe<string>> SendFriendRequest(string username, string friendUsername)
        {
            var maybe = new Maybe<string>();

            var myUser = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);
            var friendUser = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == friendUsername);

            if (myUser == null || friendUser == null)
            {
                maybe.SetException("Could not find username");
                return maybe;
            }
            var any = await _dbContext.Friends.AnyAsync(friend => (friend.UserId == myUser.UserId && friend.FriendUserId == friendUser.UserId) || (friend.UserId == friendUser.UserId && friend.FriendUserId == myUser.UserId));

            if (!any)
            {
                var friendRequest = new DBFriend
                {
                    UserId = myUser.UserId,
                    FriendUserId = friendUser.UserId,
                    Status = FriendRequestStatus.Pending
                };

                _dbContext.Friends.Add(friendRequest);
                await _dbContext.SaveChangesAsync();
                maybe.SetSuccess("Friend request sent");
            }
            else
            {
                maybe.SetException("Friend relationship already exists");
            }


            return maybe;
        }

        public async Task<Maybe<string>> AcceptFriendRequest(string myUsername, string friendUsername)
        {
            var maybe = new Maybe<string>();
            var myUser = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == myUsername);
            var friendUser = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == friendUsername);

            if (myUser == null || friendUser == null)
            {
                maybe.SetException("Could not find username");
                return maybe;
            }
            var friendRequest = await _dbContext.Friends.FirstOrDefaultAsync(e => e.UserId == friendUser.UserId && e.FriendUserId == myUser.UserId);
            if (friendRequest == null || friendRequest.Status != FriendRequestStatus.Pending)
            {
                maybe.SetException("Rquest already accepted");
            }
            else
            {
                friendRequest.Status = FriendRequestStatus.Accepted;
                await _dbContext.SaveChangesAsync();
                maybe.SetSuccess("Request accepted");
            }

            return maybe;
        }

        public async Task<Maybe<string>> DeclineFriendRequest(string myUsername, string friendUsername)
        {
            var maybe = new Maybe<string>();
            var myUser = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == myUsername);
            var friendUser = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == friendUsername);

            if (myUser == null || friendUser == null)
            {
                maybe.SetException("Could not find username");
                return maybe;
            }
            var friendRequest = await _dbContext.Friends.FirstOrDefaultAsync(e => e.UserId == friendUser.UserId && e.FriendUserId == myUser.UserId);
            if (friendRequest == null || friendRequest.Status != FriendRequestStatus.Pending)
            {
                maybe.SetException("Rquest already accepted");
            }
            else
            {
                _dbContext.Friends.Remove(friendRequest);
                await _dbContext.SaveChangesAsync();
                maybe.SetSuccess("Request accepted");
            }

            return maybe;
        }

        public async Task<Maybe<List<Friend>>> GetFriendRequests(string username)
        {
            var maybe = new Maybe<List<Friend>>();
            var myUser = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);

            if (myUser == null)
            {
                maybe.SetException("Could not find username");
                return maybe;
            }

            var friends = new List<Friend>();
            var friendRequests = await _dbContext.Friends.Where(e => e.FriendUserId == myUser.UserId && e.Status == FriendRequestStatus.Pending).ToListAsync();
            await AddFriends(friendRequests, myUser.UserId, friends);
            maybe.SetSuccess(friends);
            return maybe;
        }

        public async Task<Maybe<List<UserSearchResult>>> GetUsernames(string myUsername, string startsWith)
        {
            var maybe = new Maybe<List<UserSearchResult>>();
            var searchResultsList = new List<UserSearchResult>();
            if (!startsWith.IsNullOrEmpty())
            {
                var myUser = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == myUsername);
                var users = await _dbContext.Users.Where(user => user.Username.ToLower().StartsWith(startsWith)).ToListAsync();
                foreach(var user in users)
                {
                    if(user.Username == myUsername)
                    {
                        continue;
                    }

                    var friendRequestExists = await _dbContext.Friends.AnyAsync(e => e.FriendUserId == user.UserId && e.UserId == myUser.UserId);
                    if (friendRequestExists)
                    {
                        searchResultsList.Add(new UserSearchResult()
                        {
                            AlreadyAdded = true,
                            Username = user.Username,
                            Rank = user.Rank
                        });
                    }
                    else
                    {
                        searchResultsList.Add(new UserSearchResult()
                        {
                            AlreadyAdded = false,
                            Username = user.Username,
                            Rank = user.Rank
                        });
                    }
                }
                maybe.SetSuccess(searchResultsList);
            }
            return maybe;
        }

        public async Task<Maybe<List<Friend>>> GetFriends(string username)
        {
            var maybe = new Maybe<List<Friend>>();

            var myUser = await _dbContext.Users.FirstOrDefaultAsync(e => e.Username == username);

            if (myUser == null)
            {
                maybe.SetException("Could not find username");
                return maybe;
            }

            var friends = new List<Friend>();

            var friendsDB1 = await _dbContext.Friends.Where(e => e.UserId == myUser.UserId).ToListAsync();
            var friendsDB2 = await _dbContext.Friends.Where(e => e.FriendUserId == myUser.UserId).ToListAsync();

            foreach (var friendDB in friendsDB1)
            {
                if (!friends.Any(e => e.UserID == myUser.UserId))
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.UserId == friendDB.FriendUserId);
                    if (user != null)
                    {
                        friends.Add(new Friend()
                        {
                            UserID = user.UserId,
                            Username = user.Username,
                            Score = user.Rank,
                            IsOnline = _connectionManager.UserConnections.ContainsKey(user.Username)
                        }); ;
                    }
                }
            }

            foreach (var friendDB in friendsDB2)
            {
                if (!friends.Any(e => e.UserID == myUser.UserId))
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.UserId == friendDB.UserId);
                    if (user != null)
                    {
                        friends.Add(new Friend()
                        {
                            UserID = user.UserId,
                            Username = user.Username,
                            Score = user.Rank,
                            IsOnline = _connectionManager.UserConnections.ContainsKey(user.Username)
                        });
                    }
                }
            }

            maybe.SetSuccess(friends);
            return maybe;
        }

        private async Task AddFriends(List<DBFriend> friendsDB, int ofUserId, List<Friend> friends)
        {
            foreach (var friendDB in friendsDB)
            {
                if (!friends.Any(e => e.UserID == ofUserId))
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(e => e.UserId == friendDB.UserId);
                    if (user != null)
                    {
                        friends.Add(new Friend()
                        {
                            UserID = friendDB.UserId,
                            Username = user.Username,
                            Score = user.Rank
                        });
                    }
                }
            }
        }

        /*public async Task<bool> SendGameInvitation(int senderUserId, int receiverUserId)
        {
            var invitation = new DBGameInvitation
            {
                SenderUserId = senderUserId,
                ReceiverUserId = receiverUserId,
                Status = FriendRequestStatus.Pending
            };

            _dbContext.GameInvitations.Add(invitation);
            await _dbContext.SaveChangesAsync();
            return true;
        }*/
    }
}
