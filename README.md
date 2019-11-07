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
