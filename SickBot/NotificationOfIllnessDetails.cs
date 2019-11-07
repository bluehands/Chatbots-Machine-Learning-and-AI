using System;
using Microsoft.Bot.Schema;

namespace SickBot
{
    public class NotificationOfIllnessDetails
    {
        public string Text { get; set; }
        public DateTime? SickUntil { get; set; }
    }
}