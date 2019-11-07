using Microsoft.Bot.Schema;

namespace SickBot
{
    public class UserData
    {
        public bool HasShownToken { get; set; }
        public TokenResponse TokenResponse { get; set; }
    }
}