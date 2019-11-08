// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace SickBot.Dialogs
{
    public class NotificationOfIllnessDialog : CancelAndHelpDialog
    {
        public NotificationOfIllnessDialog() : base(nameof(NotificationOfIllnessDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt), null, "de-de"));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                SickUntilDateStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> SickUntilDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Options;

            if (!notificationOfIllnessDetails.SickUntil.HasValue)
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), notificationOfIllnessDetails, cancellationToken);
            }

            return await stepContext.NextAsync(notificationOfIllnessDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Result;

            var messageText = $"Alles klar. Du bist bis zum {notificationOfIllnessDetails.SickUntil?.ToString("dd.MM.yyyy")} krank und nicht im Büro. Habe ich das richtig verstanden?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }
       
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Options;
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

    }
}
