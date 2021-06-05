using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Graph;

namespace SickBot
{
    public class MailClient
    {
        private readonly GraphServiceClient m_GraphClient;

        public MailClient(TokenResponse tokenResponse)
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
        public async Task SendMail(IEnumerable<string> recipientAddress, string subject, string message)
        {
            var mailMessage = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = message
                },
                ToRecipients = new List<Recipient>(recipientAddress.Select(r => new Recipient { EmailAddress = new EmailAddress { Address = r } }))
            };
            //await m_GraphClient.Me.SendMail(mailMessage, false).Request().PostAsync();
        }
    }
}