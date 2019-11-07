// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace SickBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly NotificationOfIllnessRecognizer m_LuisRecognizer;
        protected readonly ILogger m_Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(NotificationOfIllnessRecognizer luisRecognizer, NotificationOfIllnessDialog notificationOfIllnessDialog , ILogger<MainDialog> logger) : base(nameof(MainDialog))
        {
            m_LuisRecognizer = luisRecognizer;
            m_Logger = logger;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(notificationOfIllnessDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }
        
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!m_LuisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }
            var messageText = stepContext.Options?.ToString() ?? "Wie kann ich Dir helfen?\nSage sowas wie \"Ich bin bis morgen krank\"";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!m_LuisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the NotificationOfIllnessDialog path with an empty NotificationOfIllnessDetails instance.
                return await stepContext.BeginDialogAsync(nameof(NotificationOfIllnessDialog), new NotificationOfIllnessDetails(), cancellationToken);
            }

            // Call LUIS and gather any potential notification of illness details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await m_LuisRecognizer.RecognizeAsync<Luis.SickBot>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case Luis.SickBot.Intent.NotificationOfIllness:

                    // Initialize NotificationOfIllnessDetails with any entities we may have found in the response.
                    var notificationOfIllnessDetails = new NotificationOfIllnessDetails()
                    {
                        Text = luisResult.Text,
                        SickUntil = luisResult.SickUntilTimex,
                    };

                    // Run the Dialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(NotificationOfIllnessDialog), notificationOfIllnessDetails, cancellationToken);
                case Luis.SickBot.Intent.Utilities_Help:
                    var welcomeCard = AdaptiveCard.CreateAttachment("SickBot.Cards.HelpCard_de.json");
                    var response = MessageFactory.Attachment(welcomeCard);
                    await stepContext.Context.SendActivityAsync(response, cancellationToken);
                    break;
                case Luis.SickBot.Intent.Utilities_Stop:
                    var cancelMsgText = "Ich breche jetzt ab.";
                    var cancelMessage = MessageFactory.Text(cancelMsgText, cancelMsgText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(cancelMessage, cancellationToken);
                    return await stepContext.CancelAllDialogsAsync(cancellationToken);
                default:
                    // Catch all for unhandled intents
                    var notUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})";
                    var notUnderstandMessage = MessageFactory.Text(notUnderstandMessageText, notUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(notUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart the main dialog with a different message the second time around
            var promptMessage = stepContext.Result == null ? "OK - dann nochmal, was kann ich für Dich tun?" : "Was kann ich noch für Dich tun?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
