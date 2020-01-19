using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using System.Linq;

namespace SickBot
{
    public class Colleagues
    {
        private readonly GraphServiceClient m_GraphClient;
        private const int NumberOfMembersOfHugeTeamsToIgnore = 10;

        public Colleagues(TokenResponse tokenResponse)
        {
            m_GraphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(requestMessage =>
                {
                    requestMessage
                        .Headers
                        .Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        tokenResponse.Token);

                    return Task.FromResult(0);
                }));
        }

        public async Task<IEnumerable<string>> GetMyColleaguesMailAddresses()
        {
            return await GetMyColleagueAttributes(UserAttribute.Mail);
        }
        public async Task<IEnumerable<string>> GetMyColleaguesName()
        {
            return await GetMyColleagueAttributes(UserAttribute.GivenName);
        }

        private async Task<IEnumerable<string>> GetMyColleagueAttributes(UserAttribute attribute)
        {
            var mailAddresses = new List<string>();
            var joinedTeams = await m_GraphClient.Me.JoinedTeams
                .Request()
                .GetAsync();

            foreach (var joinedTeam in joinedTeams)
            {
                mailAddresses.AddRange(await GetAttributeOfTeam(joinedTeam, attribute));
            }

            return mailAddresses.Distinct();
        }

        private async Task<IEnumerable<string>> GetAttributeOfTeam(Group joinedTeam, UserAttribute attribute)
        {
            var members = await m_GraphClient.Groups[joinedTeam.Id].Members
                .Request()
                .GetAsync();
            if (members.Count > NumberOfMembersOfHugeTeamsToIgnore)
            {
                return new string[] { };
            }
            return members.Select(m =>
            {
                var u = (User)m;
                return attribute switch
                {
                    UserAttribute.Mail => u.Mail,
                    UserAttribute.GivenName => u.GivenName,
                    _ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null)
                };
            });
        }
    }

    internal enum UserAttribute
    {
        Mail,
        GivenName
    }
}