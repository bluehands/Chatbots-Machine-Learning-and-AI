using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SickBot
{
    public class Appointments
    {
        private readonly string m_Token;
        private readonly DateTime m_AppointmentsUntil;

        public Appointments(string token, DateTime appointmentsUntil)
        {
            m_Token = token;
            m_AppointmentsUntil = appointmentsUntil;
        }

        public List<Appointment> GetAppointments()
        {
            return new List<Appointment>
            {
                new Appointment(m_Token) {Title = "Treffen mit Lars", Start = GetFullDate(DateTime.Now.AddHours(2)), End = GetFullDate(DateTime.Now.AddHours(3))},
                new Appointment(m_Token) {Title = "Daily", Start = GetFullDate(DateTime.Now.AddHours(25)), End =GetFullDate( DateTime.Now.AddHours(26))}
            };
        }

        public DateTime GetFullDate(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
        }
    }
    public class Appointment
    {
        private readonly string m_Token;

        public Appointment(string token)
        {
            m_Token = token;
        }
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public void Cancel()
        {

        }

    }
}