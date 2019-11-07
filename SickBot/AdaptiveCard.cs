using System.IO;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace SickBot
{
    public static class AdaptiveCard
    {
        public static Attachment CreateAttachment(string cardResourcePath)
        {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }
    }
}