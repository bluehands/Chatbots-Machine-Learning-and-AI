using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Exchange.WebServices.Data;

namespace SickBot
{
    public class ExchangeClient
    {
        private readonly ExchangeService m_Service;
        public ExchangeClient(Uri serviceUrl, NetworkCredential serviceCredentials, string impersonatedUserPrincipleName)
        {
            m_Service = new ExchangeService(ExchangeVersion.Exchange2013)
            {
                Credentials = serviceCredentials,
                UseDefaultCredentials = false,
                Url = serviceUrl,
                ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.PrincipalName, impersonatedUserPrincipleName)
            };
        }
        public IEnumerable<Meeting> GetMeetings(DateTime startDate, DateTime endDate)
        {
            var calendar = CalendarFolder.Bind(m_Service, WellKnownFolderName.Calendar, new PropertySet());
            var cView = new CalendarView(startDate, endDate)
            {
                PropertySet = new PropertySet(ItemSchema.Subject, AppointmentSchema.Start, AppointmentSchema.End, AppointmentSchema.IsMeeting, AppointmentSchema.Organizer, AppointmentSchema.MyResponseType)
            };
            FindItemsResults<Microsoft.Exchange.WebServices.Data.Appointment> appointments = calendar.FindAppointments(cView);
            foreach (Microsoft.Exchange.WebServices.Data.Appointment a in appointments)
            {
                if (!a.IsMeeting) { continue; }

                var meeting = new Meeting(a.Id.ToString())
                {
                    Subject = a.Subject,
                    Start = a.Start,
                    End = a.End,
                    Organizer = a.Organizer.Name,
                    IsOrganizer = a.MyResponseType == MeetingResponseType.Organizer
                };
                yield return meeting;
            }
        }
        public void CancelMeeting(string meetingId, string cancelMessage)
        {
            Microsoft.Exchange.WebServices.Data.Appointment meeting = Microsoft.Exchange.WebServices.Data.Appointment.Bind(m_Service, meetingId, new PropertySet(AppointmentSchema.MyResponseType));
            if (meeting.MyResponseType == MeetingResponseType.Organizer)
            {
                meeting.CancelMeeting(cancelMessage);
            }
            else if (meeting.MyResponseType == MeetingResponseType.Accept || meeting.MyResponseType == MeetingResponseType.NoResponseReceived || meeting.MyResponseType == MeetingResponseType.Tentative)
            {
                var declineMessage = meeting.CreateDeclineMessage();
                declineMessage.Body = new MessageBody(cancelMessage);
                declineMessage.SendAndSaveCopy();
            }
        }
        public void SendMail(IEnumerable<string> toRecipients, string subject, string body)
        {
            EmailMessage email = new EmailMessage(m_Service);
            email.ToRecipients.AddRange(toRecipients);
            email.Subject = subject;
            email.Body = new MessageBody(body);
            email.Send();
        }
    }
    public class Meeting
    {
        public Meeting(string meetingId)
        {
            MeetingId = meetingId;
        }
        public string MeetingId { get; }
        public string Subject { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsOrganizer { get; set; }
        public string Organizer { get; set; }
    }

    public class ExchangeSettings
    {
        public string ConnectionUrl { get; set; }
        public string ConnectionUserName { get; set; }
        public string ConnectionUserPassword { get; set; }
    }
}