using System;
using Microsoft.Bot.Schema;

namespace SickBot
{
    public class ExchangeMailClient
    {
        private readonly ExchangeClient m_ExchangeClient;

        public ExchangeMailClient(TokenResponse tokenResponse, ExchangeSettings settings) 
        {
            m_ExchangeClient = new ExchangeClient(new Uri(settings.ConnectionUrl), new System.Net.NetworkCredential(settings.ConnectionUserName, settings.ConnectionUserPassword), tokenResponse.GetUPNClaim().Value);
        }
        public void SendMail(string recipientAddress,string subject, string message)
        {
            m_ExchangeClient.SendMail(new[] { recipientAddress }, "Krankmeldung", message);
        }
    }
}