// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SickBot.Dialogs
{
    public class MainDialog : LogoutDialog
    {
        private readonly NotificationOfIllnessRecognizer m_LuisRecognizer;
        private readonly IStatePropertyAccessor<UserData> m_UserStateAccessors;
        protected readonly ILogger m_Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(NotificationOfIllnessRecognizer luisRecognizer, NotificationOfIllnessDialog notificationOfIllnessDialog, UserState userState, IConfiguration configuration, ILogger<MainDialog> logger) : base(nameof(MainDialog), configuration["ConnectionName"])
        {
            m_LuisRecognizer = luisRecognizer;
            m_UserStateAccessors = userState.CreateProperty<UserData>(nameof(UserData));
            m_Logger = logger;
            AddDialog(new OAuthPrompt(nameof(OAuthPrompt), new OAuthPromptSettings
            {
                ConnectionName = ConnectionName,
                Text = "Bitte melde Dich an",
                Title = "Bei Azure AD anmelden",
                Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
            }));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(notificationOfIllnessDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                LoginStepAsync,
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!m_LuisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);
                return await stepContext.CancelAllDialogsAsync(false, null, null, cancellationToken);
            }
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }
        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself.
            if (stepContext.Result is TokenResponse tokenResponse)
            {
                var conversationData = await m_UserStateAccessors.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
                conversationData.TokenResponse = tokenResponse;
                if (!conversationData.HasShownToken)
                {
                    conversationData.HasShownToken = true;
                    await m_UserStateAccessors.SetAsync(stepContext.Context, conversationData, cancellationToken);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Du bist angemeldet"), cancellationToken);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(tokenResponse.Token), cancellationToken);
                }
                return await stepContext.NextAsync(tokenResponse, cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Die Anmeldung ist fehlgeschlagen. Bitte versuche es nochmal."), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userData = await m_UserStateAccessors.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);
            var messageText = stepContext.Options?.ToString() ?? $"{userData.TokenResponse.GetGivenNameClaim()?.Value}: Wie kann ich Dir helfen?\nSage sowas wie \"Ich bin bis morgen krank\"";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Call LUIS and gather any potential notification of illness details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await m_LuisRecognizer.RecognizeAsync<Luis.SickBot>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case Luis.SickBot.Intent.NotificationOfIllness:
                    // Initialize NotificationOfIllnessDetails with any entities we may have found in the response.
                    var userData = await m_UserStateAccessors.GetAsync(stepContext.Context, () => new UserData(), cancellationToken);

                    var notificationOfIllnessDetails = new NotificationOfIllnessDetails
                    {
                        Text = luisResult.Text,
                        SickUntil = luisResult.SickUntilTimex,
                        TokenResponse = userData.TokenResponse
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
