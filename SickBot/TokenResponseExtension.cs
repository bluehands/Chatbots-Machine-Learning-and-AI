using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Bot.Schema;

namespace SickBot
{
    public static class TokenResponseExtension
    {
        public static Claim GetClaim(this TokenResponse tokenResponse, string claimName)
        {
            var jwtToken = new JwtSecurityToken(tokenResponse.Token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type.Equals(claimName));
        }
        public static Claim GetNameClaim(this TokenResponse tokenResponse)
        {
            return GetClaim(tokenResponse, "name");
        }
        public static Claim GetGivenNameClaim(this TokenResponse tokenResponse)
        {
            return GetClaim(tokenResponse, "given_name");
        }
        public static Claim GetUPNClaim(this TokenResponse tokenResponse)
        {
            return GetClaim(tokenResponse, "upn");
        }
    }
}