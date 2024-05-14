using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaHub.Models
{
    public class PaypalConfiguration
    {
        public static readonly string ClientID = "AWi5BBvtCwL002TGL2CvwBTLREMprhhcKG3AawQcIiW1lp_zWobEmkQqS4dZuIS99WjN8evpnPdNZrCx";
        public static readonly string ClientSecret = "EMOe9SidK4he2iSFLaVw99gtAHYzs3AU2222-Em5zxgjpxAnRdsfSFzBtyO0sPhNgif7hMnY7Vqxnkys";

        public static Dictionary<string, string> Configuration()
        {
            return new Dictionary<string, string>
            {
                {"mode", "sandbox"}
            };
        }

        private static string GetAccessToken()
        {
            string accessToken = "";

            accessToken = new OAuthTokenCredential(ClientID, ClientSecret, new Dictionary<string, string>
            {
                {"mode", "sandbox"}
            }).GetAccessToken();

            return accessToken;
        }

        public static APIContext GetAPIContext()
        {
            APIContext context = new APIContext(GetAccessToken());
            context.Config = Configuration();
            return context;
        }
    }
}
