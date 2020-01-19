using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Schema;

namespace SickBot
{
    public class Appointments 
    {
        private readonly ExchangeClient m_ExchangeClient;

        public Appointments(TokenResponse tokenResponse, ExchangeSettings settings) 
        {
            m_ExchangeClient = new ExchangeClient(new Uri(settings.ConnectionUrl), new System.Net.NetworkCredential(settings.ConnectionUserName, settings.ConnectionUserPassword), tokenResponse.GetUPNClaim().Value);
        }
        public List<Appointment> GetAppointments(DateTime meetingsUntil)
        {
            return m_ExchangeClient.GetMeetings(DateTime.Today, meetingsUntil).Select(
                m => new Appointment
                {
                    Id = m.MeetingId,
                    Subject = m.Subject,
                    Start = m.Start,
                    End = m.End
                }).ToList();
        }

        public void CancelAllAppointments(DateTime meetingsUntil, string cancelMessage)
        {
            foreach (var meeting in m_ExchangeClient.GetMeetings(DateTime.Today, meetingsUntil))
            {
                m_ExchangeClient.CancelMeeting(meeting.MeetingId, cancelMessage);
            }
        }
    }

    public class Appointment
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

    }
}
