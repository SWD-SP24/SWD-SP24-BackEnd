using FirebaseAdmin.Auth;

namespace SWD392.Service
{
    public class AuthenticationService
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
    }
}
