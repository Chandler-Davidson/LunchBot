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
            MenuManager = new MenuManager() { FilePath = @".\menu.pdf" };
            LunchMenu = MenuManager.LunchMenu;

            context.Wait(this.MessageReceivedAsync);
        }

        public async virtual Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            var welcomeMessage = context.MakeMessage();
            welcomeMessage.Text = $"Hey, {context.Activity.Recipient.Name} I'm lunch bot.";

            await context.PostAsync(welcomeMessage);

            await this.CollectUserInput(context);
        }

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
                replyMessage.Text = "Today's menu:\n" + GetDaysMenu(dayOfWeek);
            }
            else if (message.Contains("tomorrow"))
            {
                replyMessage.Text = GetDaysMenu(dayOfWeek + 1);
            }
            else if (message.Contains("week"))
            {
                replyMessage.Text = "Here's the rest of the week:";
                await context.PostAsync(replyMessage);

                for (int i = dayOfWeek; i < 5; i++)
                {
                    replyMessage.Text = $"{Enum.GetName(typeof(DayOfWeek), i + 1)}:\n{GetDaysMenu(i)}\n\n\n";
                    await context.PostAsync(replyMessage);
                }

                replyMessage.Text = "";

            }
            else if (message.Contains("whole menu"))
            {
                Attachment attachment = GetMenuDocument(MenuManager.FilePath);
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
                MenuManager.FilePath,
                null,
                "MegaBytes Menu");
        }

        // TODO: Move into the menu object as a .ToString()
        private string GetMenuWeek()
        {
            var fullMenu = "";

            for (int i = 0; i < 5; i++)
            {
                fullMenu += $"{Enum.GetName(typeof(DayOfWeek), i)}:\n{GetDaysMenu(i)}";
            }

            return fullMenu;
        }

        // TODO: Move into the menu object as a .ToString()
        private string GetDaysMenu(int dayOfWeek)
        {
            var today = LunchMenu.Menu[dayOfWeek];

            var stationStrs = today.FoodStations.Select(s => $"{s.FoodName} ${s.Price}\n");

            return string.Join("\n", stationStrs);
        }
    }
}