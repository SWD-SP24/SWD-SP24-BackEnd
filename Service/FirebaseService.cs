using FirebaseAdmin.Auth;

namespace SWD392.Service
{
    public class FirebaseService
    {
        public async Task<string> RegisterAsync(string username, string password)
        {
            var userArgs = new UserRecordArgs
            {
                Email = username,
                Password = password
            };
            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);

            return userRecord.Uid;
        }

        public async Task DeleteAsync(string uid)
        {
            await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
        }
    }
}
