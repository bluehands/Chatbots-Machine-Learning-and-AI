namespace SickBot
{
    public class BackOffice
    {
        private readonly string m_Token;

        public BackOffice(string token)
        {
            m_Token = token;
        }

        public BackOfficeMember GetBackOfficeMember()
        {
            return new BackOfficeMember {Name = "Lars", MailAddress = "lk@bluehands.de"};
        }
    }

    public class BackOfficeMember
    {
        public string Name { get; set; }
        public string MailAddress { get; set; }
    }
}