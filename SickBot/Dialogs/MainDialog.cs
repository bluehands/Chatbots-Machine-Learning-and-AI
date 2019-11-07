// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace SickBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        protected readonly ILogger m_Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ILogger<MainDialog> logger) : base(nameof(MainDialog))
        {
            m_Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                MainStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = stepContext.Options?.ToString() ?? $"Wie kann ich Dir helfen?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.AcceptingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }
        private async Task<DialogTurnResult> MainStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(stepContext.Result.ToString(), stepContext.Result.ToString(), InputHints.IgnoringInput), cancellationToken);
            return await stepContext.NextAsync(stepContext.Result.ToString(), cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart the main dialog with a different message the second time around
            var promptMessage = stepContext.Result == null ? "OK - dann nochmal, was kann ich für Dich tun?" : "Was kann ich noch für Dich tun?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
