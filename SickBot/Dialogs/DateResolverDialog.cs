// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;

namespace SickBot.Dialogs
{
    public class DateResolverDialog : CancelAndHelpDialog
    {
        private const string PromptMsgText = "Wie lange bist Du krank?";
        private const string RepromptMsgText = "Ich konnte das Datum nicht verstehen. Gib bitte ein Datum inklusive Tag, monat und jahr an.";

        public DateResolverDialog(string id = null)
            : base(id ?? nameof(DateResolverDialog))
        {
            AddDialog(new DateTimePrompt(nameof(DateTimePrompt), DateTimePromptValidator));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Options;

            var promptMessage = MessageFactory.Text(PromptMsgText, PromptMsgText, InputHints.ExpectingInput);
            var repromptMessage = MessageFactory.Text(RepromptMsgText, RepromptMsgText, InputHints.ExpectingInput);

            if (!Recognizer.TryGetDate(notificationOfIllnessDetails.Text, Culture.German, out DateTime sickUntilDate))
            {
                // We were not given any date at all so prompt the user.
                return await stepContext.PromptAsync(nameof(DateTimePrompt),
                    new PromptOptions
                    {
                        Prompt = promptMessage,
                        RetryPrompt = repromptMessage,
                    }, cancellationToken);
            }

            notificationOfIllnessDetails.SickUntil = sickUntilDate;
            return await stepContext.NextAsync(notificationOfIllnessDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is List<DateTimeResolution> resolution)
            {
                return await stepContext.EndDialogAsync(new NotificationOfIllnessDetails { SickUntil = DateTime.Parse(resolution[0].Timex) }, cancellationToken);
            }
            var notificationOfIllnessDetails = (NotificationOfIllnessDetails)stepContext.Result;
            return await stepContext.EndDialogAsync(notificationOfIllnessDetails, cancellationToken);
        }

        private static Task<bool> DateTimePromptValidator(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            if (Recognizer.TryGetDate(promptContext.Context.Activity.Text, Culture.German, out DateTime sickUntilDate))
            {
                promptContext.Recognized.Value = new List<DateTimeResolution> { new DateTimeResolution { Timex = sickUntilDate.ToString("dd-MM-yyyy") } };
                promptContext.Recognized.Succeeded = true;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
