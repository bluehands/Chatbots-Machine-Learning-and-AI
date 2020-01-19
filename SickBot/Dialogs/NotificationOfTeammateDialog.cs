using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace SickBot.Dialogs
{
    public class NotificationOfTeammateDialog : CancelAndHelpDialog
    {
        private readonly ExchangeSettings m_ExchangeSettings;

        public NotificationOfTeammateDialog(ExchangeSettings exchangeSettings) : base(nameof(NotificationOfTeammateDialog))
        {
            m_ExchangeSettings = exchangeSettings;
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt), null, "de-de"));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NotifyBackOfficeAsync,
                PromptNotifyColleaguesAsync,
                NotifyColleaguesAsync,
                ShowAppointmentsAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> NotifyBackOfficeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Options;
            var backOffice = new BackOffice(notificationOfIllnessDetails.TokenResponse);
            var backOfficeMember = backOffice.GetBackOfficeMember();
            var mailClient = new ExchangeMailClient(notificationOfIllnessDetails.TokenResponse, m_ExchangeSettings);
            var message = $"Dies ist eine automatisch generierte Nachricht. {notificationOfIllnessDetails.TokenResponse.GetNameClaim()?.Value} ist bis zum {notificationOfIllnessDetails.SickUntil.GetValueOrDefault():dd.MM.yyyy} krank und ist nicht im Büro."; ;
            mailClient.SendMail(new[] { backOfficeMember.MailAddress }, "Krankmeldung", message);
            var msg = $"Ich habe {backOfficeMember.Name} vom Backoffice eine Mail ({backOfficeMember.MailAddress}) gesendet, daß Du bis zum {notificationOfIllnessDetails.SickUntil?.ToString("dd.MM.yyyy")} krank bist.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg, msg, InputHints.IgnoringInput), cancellationToken);
            return await stepContext.NextAsync(notificationOfIllnessDetails, cancellationToken);
        }
        private async Task<DialogTurnResult> PromptNotifyColleaguesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Options;
            var colleagues = new Colleagues(notificationOfIllnessDetails.TokenResponse);
            var colleagueNames = await colleagues.GetMyColleaguesName();
            var messageText = $"Soll ich Deine Kollegen {string.Join(", ", colleagueNames)} bescheid geben?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }
        private async Task<DialogTurnResult> NotifyColleaguesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Options;
            var sendNotificationToColleagues = (bool)stepContext.Result;
            if (sendNotificationToColleagues)
            {
                var colleagues = new Colleagues(notificationOfIllnessDetails.TokenResponse);
                var colleagueMailAddresses = await colleagues.GetMyColleaguesMailAddresses();
                var mailClient = new ExchangeMailClient(notificationOfIllnessDetails.TokenResponse, m_ExchangeSettings);
                var message = $"Dies ist eine automatisch generierte Nachricht. {notificationOfIllnessDetails.TokenResponse.GetNameClaim()?.Value} ist bis zum {notificationOfIllnessDetails.SickUntil.GetValueOrDefault():dd.MM.yyyy} krank und ist nicht im Büro.";
                mailClient.SendMail(colleagueMailAddresses, "Krankmeldung", message);

                var msg = $"Ich habe Deine Kollegen eine Mail gesendet, daß Du bis zum {notificationOfIllnessDetails.SickUntil?.ToString("dd.MM.yyyy")} krank bist.";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg, msg, InputHints.IgnoringInput), cancellationToken);
            }

            return await stepContext.NextAsync(notificationOfIllnessDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowAppointmentsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Result;
            var appointments = new Appointments(notificationOfIllnessDetails.TokenResponse, m_ExchangeSettings);
            var appointmentList = appointments.GetAppointments(notificationOfIllnessDetails.SickUntil.GetValueOrDefault());
            if (appointmentList.Count == 0)
            {
                var noAppointmentsMsg = "Du hast keine Termine die ich absagen muss.";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(noAppointmentsMsg, noAppointmentsMsg, InputHints.IgnoringInput), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            var appointmentsMsg = "Du hast folgende Termine die ich absagen kann.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(appointmentsMsg, appointmentsMsg, InputHints.IgnoringInput), cancellationToken);
            // Reply to the activity we received with an activity.
            var reply = MessageFactory.Attachment(new List<Attachment>());
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            foreach (var appointment in appointmentList)
            {
                reply.Attachments.Add(GetAppointmentCard(appointment).ToAttachment());
            }
            // Send the card(s) to the user as an attachment to the activity
            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            var messageText = "Soll ich diese absagen?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);


        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Options;
                var cancelMessage = $"Dies ist eine automatisch generierte Nachricht. {notificationOfIllnessDetails.TokenResponse.GetNameClaim()?.Value} ist bis zum {notificationOfIllnessDetails.SickUntil.GetValueOrDefault():dd.MM.yyyy} krank und kann nicht teilnehmen.";
                var appointments = new Appointments(notificationOfIllnessDetails.TokenResponse, m_ExchangeSettings);
                appointments.CancelAllAppointments(notificationOfIllnessDetails.SickUntil.GetValueOrDefault(), cancelMessage);
                var msg = "Ich habe die Termine abgesagt.";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg, msg, InputHints.IgnoringInput), cancellationToken);
                return await stepContext.NextAsync(stepContext.Options, cancellationToken);
            }
            var msg2 = "Alles klar. Du kümmerst Dich selbst um die Termine.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg2, msg2, InputHints.IgnoringInput), cancellationToken);
            return await stepContext.EndDialogAsync(stepContext.Options, cancellationToken);
        }
        private static ThumbnailCard GetAppointmentCard(Appointment appointment)
        {
            var heroCard = new ThumbnailCard
            {
                Title = appointment.Subject,
                Subtitle = $"{appointment.Start}",
                Text = "",
                Images = new List<CardImage> { new CardImage("https://sickbot.z6.web.core.windows.net/calendar128.png") }
            };

            return heroCard;
        }

    }
}
