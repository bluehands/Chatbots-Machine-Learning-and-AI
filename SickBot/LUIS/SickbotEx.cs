using System;
using Microsoft.Recognizers.Text;
using SickBot;

namespace Luis
{
    public partial class SickBot
    {
        public DateTime? SickUntilTimex
        {
            get
            {
                if (Recognizer.TryGetDate(Text, Culture.German, out DateTime sickUntilDate))
                {
                    return sickUntilDate;
                }

                return null;
            }
        }
    }
}