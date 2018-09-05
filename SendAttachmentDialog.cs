namespace SendAttachmentBot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using System.Web;
    using System.Linq;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;

    using PDFParser;

    [Serializable]
    internal class SendAttachmentDialog : IDialog<object>
    {
        public static MenuManager MenuManager { get; set; }
        public static LunchMenu LunchMenu { get; set; }

        public async Task StartAsync(IDialogContext context)
        {
            // TODO: Asynchronous menu
            // 1. Make menu fetch asnyc
            // 2. Update MenuManager with a HasLatest prop
            // 3. if (!menuManager.HasLatest) 
            //      push message("Lemme go grab the menu");
            MenuManager = new MenuManager()
            { DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PDFParser\" };

            var welcomeMessage = context.MakeMessage();
            welcomeMessage.Text = $"Hey, {context.Activity.Recipient.Name} I'm lunch bot.";
            await context.PostAsync(welcomeMessage);

            context.Wait(this.MessageReceivedAsync);
        }

        public async virtual Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            if (!MenuManager.HasCurrentMenu)
            {
                var fetchingMenuMessage = context.MakeMessage();
                fetchingMenuMessage.Text = $"Sorry, lemme grab the menu...";
                await context.PostAsync(fetchingMenuMessage);

                var newMenu = GetNewMenu();

                LunchMenu = await newMenu;
            }
            else
            {
                LunchMenu = MenuManager.LunchMenu;
            }





            await this.CollectUserInput(context);
        }

        private async Task<LunchMenu> GetNewMenu()
        {
            return MenuManager.LunchMenu;
        }

        // TODO: 
        // Asnyc fetching the menu.
        // It'd be cool to see the bot warn the user if
        // PDFParser had to fetch the menu.
        public async Task CollectUserInput(IDialogContext context)
        {
            PromptDialog.Text(
                context,
                this.ProcessUserInput,
                "What can I help you with?",
                "Sorry, didn't catch that.");
        }

        private async Task ProcessUserInput(IDialogContext context, IAwaitable<string> result)
        {
            // TODO: Can we implement cognitive services to improve conversation?
            // Conversion from DayOfWeek to int is hacky
            var message = await result;

            var replyMessage = context.MakeMessage();

            var dayOfWeek = (int)DateTime.Now.DayOfWeek - 1;

            if (message.Contains("today"))
            {
                replyMessage.Text = "Today's menu:\n" + LunchMenu[dayOfWeek].ToString();
            }
            else if (message.Contains("tomorrow"))
            {
                replyMessage.Text = LunchMenu[dayOfWeek + 1].ToString();
            }
            else if (message.Contains("week"))
            {
                replyMessage.Text = "Here's the rest of the week:";
                await context.PostAsync(replyMessage);

                LunchMenu.ForEach(async x =>
                    await context.PostAsync(context.MakeMessage().Text = x.ToString()));
            }
            else if (message.Contains("whole menu"))
            {
                Attachment attachment = GetMenuDocument(MenuManager.DirectoryPath);
                replyMessage.Text = "Here you are!";
                replyMessage.Attachments = new List<Attachment> { attachment };
            }
            else
            {
                replyMessage.Text = "I didn't catch that.";
            }

            await context.PostAsync(replyMessage);

            //await this.DisplayOptionsAsync(context);
            await this.CollectUserInput(context);
        }

        // Is there a way to show inline? 
        // Convert to png?
        private Attachment GetMenuDocument(object filePath)
        {
            return new Attachment(
                "image/pdf",
                MenuManager.DirectoryPath,
                null,
                "MegaBytes Menu");
        }
    }
}