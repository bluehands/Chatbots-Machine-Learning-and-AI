﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;

namespace SickBot
{
    public class NotificationOfIllnessRecognizer : IRecognizer
    {
        private readonly LuisRecognizer m_Recognizer;

        public NotificationOfIllnessRecognizer(IConfiguration configuration)
        {
            var luisIsConfigured = !string.IsNullOrEmpty(configuration["LuisAppId"]) && !string.IsNullOrEmpty(configuration["LuisAPIKey"]) && !string.IsNullOrEmpty(configuration["LuisAPIHostName"]);
            if (luisIsConfigured)
            {
                var luisApplication = new LuisApplication(
                    configuration["LuisAppId"],
                    configuration["LuisAPIKey"],
                    "https://" + configuration["LuisAPIHostName"]);

                m_Recognizer = new LuisRecognizer(luisApplication);
            }
        }

        // Returns true if luis is configured in the appsettings.json and initialized.
        public virtual bool IsConfigured => m_Recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await m_Recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await m_Recognizer.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}