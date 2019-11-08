# Chatbots, Machine Learning & AI

This is a cook bock for developing a chat bot for Teams. The steps are in several branches to reduce the complexity of the code.

Check out the **01_welcome_user** branch.

## Welcome User

This is the reduced *Core Bot* Template from Visual Studio. The *IBot* is the *DialogAndWelcomeBot* with the *MainDialog*. So all incoming messages are routed to *DialogAndWelcomeBot* and then to *MainDialog*.

In *DialogAndWelcomeBot* we create a adaptive card. *MainDialog* has a waterfall dialog with a prompt an echo.

## Deploy to Azure

See <https://docs.microsoft.com/de-de/azure/bot-service/bot-builder-deploy-az-cli?view=azure-bot-service-4.0&tabs=csharp> for a detailed description.

* Create a Azure AD App Registration
    * **az ad app create --display-name "SickBot" --password "AtLeastSixteenCharacters_0" --available-to-other-tenants**
    * Remember the **AppId** and **PWD** for later use
* Deploy with ARM Template from the Visual Studio Solution  
    * Navigate to the **DeploymentTemplates** Sub directory
    * **az group deployment create --name "SickBotDeployment" --resource-group "SickBot" --template-file "template-with-preexisting-rg.json" --parameters appId="<msa-app-guid>" appSecret="<msa-app-password>" botId="SickBot" newWebAppName="SickBot" newAppServicePlanName="SickBot" appServicePlanLocation="westeurope"**
* Prepare code for deployment
    * Navigate to root directory
    * **az bot prepare-deploy --lang Csharp --code-dir "." --proj-file-path "SickBot.csproj"**
* Deploy code
    * Deploy with Visual Studio
    * Update the settings
* Test in Web Chat

## Debug with ngrok

Download ngrok and start **ngrok http 3978 -host-header="localhost:3978**

## Add LUIS

Check out the **02_add_luis** branch.

We just created a bot which accepts messages. With this, we could create a command oriented UI like a console app. But we want a more smart solution. We will use LUIS to add natural language processing to catch the intent of the user.

* Add a LUIS App
* Add an Indent **NotificationOfIllness**
* Add an prebuild entity **datetimeV2**
* Add some samples
* Add a Indent **None**
* Add some samples
* Add predefined intents from the domains

Now we have to train and publish the model

* Train
* Publish Production
* Remember the items in Azure Resources for later use

We will test the model with Postman. Copy the Sample Url and paste it to Postman.

For later use of the model we will generate C# code for strongly typed enumerations of intents.

**luis export version --appId "Application ID" --versionId 0.1 --authoringKey "key"  | luisgen --stdin -cs -o "path to directory"**

In code we have update the **MainDialog**. In the *ActStepAsync*-Method we call the LUIS recognizer with the chat message we get. After that we switch over the recognized intents.

    Set a breakpoint at line 60.

For the **NotificationOfIllness** Intent we will delegate the further processing of the conversation to another dialog. Before that we will create state and pass it to the dialog **NotificationOfIllnessDialog**.

The **NotificationOfIllnessDialog** has to deal with unrecognized date and will prompt for confirmation. Unrecognized dates will be passed again to another dialog for conversation. **DateResolverDialog** uses the **DateTimeRecognizer** from bot builder to recognize and validate the date.

After recognition, we will prompt the user for confirmation.

## Add Authentication to Azure AD

In the next step we want to authenticate the user in preparation to add our domain logic.

Check out the **03_add_authentication** branch.

See <https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-authentication?view=azure-bot-service-4.0&tabs=csharp%2Cbot-oauth>

* Create a Azure AD App registration
* Go to bot settings and add the App registration
* Check connection and paste the token to <https://jwt.ms/>

In **MainDialog** we have add a new step (the first one) with a prompt to login. This is done by the **OAuthPrompt** of the framework. This dialog can be called every time we need a Identity-Token. If there is a cached token, the logon dialog will not appear.

Having a JWT-Token we can parse it and get the user identity.

We also show the token in the chat the first time after login. To remember *first time* we use the *user state*. The state is written to the blob storage.

    For authentication in Bot Emulator, ngrok has to be configured globaly. 
    Also configure all secrets in the bot configuration dialog.

## Add domain logic for notification of illness

Now we have all details of the user by authentication and recognizing indent. We will start adding the domain logic now. Notify back office and cancel appointments.

Check out the **04_add_domain** branch.

In **NotificationOfIllnessDialog** we have add a new step to delegate the dialog to the **NotificationOfTeammateDialog**. With the identity of the user and the sick date we call Microsoft Graph and gather the back office mail and all the appointments (Implemented as a mock).

We have create a *CarouselCard* and add a *ThumbnailCard* for every appointment.

## Add Teams channel

We will now add the bot to the Teams Channel.

Check out the **05_add_teams** branch.

See <https://docs.microsoft.com/de-de/azure/bot-service/bot-builder-basics-teams?view=azure-bot-service-4.0&tabs=csharp>

* Add the teams channel to the bot registration
* Download Teams AppStudio and create new app
* Add existing bot
* Remember to add *token.botframework.com* to the *Domains and Permissions*
* Install the bot
