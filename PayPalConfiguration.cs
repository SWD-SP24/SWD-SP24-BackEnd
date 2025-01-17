using PayPal.Api;

namespace SWD392
{
    public class PayPalConfiguration
    {
        public static APIContext GetAPIContext(IConfiguration configuration)
        {
            var config = new Dictionary<string, string>
{
    { "mode", configuration["PayPal:Mode"] },
    { "clientId", configuration["PayPal:ClientId"] },
    { "clientSecret", configuration["PayPal:Secret"] }
};

            try
            {
                var accessToken = new OAuthTokenCredential(
                    configuration["PayPal:ClientId"],
                    configuration["PayPal:Secret"],
                    config).GetAccessToken();

                Console.WriteLine($"Access Token: {accessToken}");
                return new APIContext(accessToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tạo APIContext: {ex.Message}");
                throw;
            }
        }

    }
}
