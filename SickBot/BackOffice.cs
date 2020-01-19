using Microsoft.Bot.Schema;

namespace SickBot
{
    public class BackOffice
    {

        public BackOffice(TokenResponse tokenResponse)
        {
            TokenResponse = tokenResponse;
        }

        public TokenResponse TokenResponse { get; }

        public virtual BackOfficeMember GetBackOfficeMember()
        {
            return new BackOfficeMember { Name = "Lars", MailAddress = "krank@bluehands.de" };
        }

    }
   

    public class BackOfficeMember
    {
        public string Name { get; set; }
        public string MailAddress { get; set; }
    }
}