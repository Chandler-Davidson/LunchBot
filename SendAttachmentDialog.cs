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

        private const string ShowInlineAttachment = "(1) Show inline attachment";
        private const string ShowUploadedAttachment = "(2) Show uploaded attachment";
        private const string ShowInternetAttachment = "(3) Show Internet attachment";

        private readonly IDictionary<string, string> options = new Dictionary<string, string>
        {
            { "1", ShowInlineAttachment },
            { "2", ShowUploadedAttachment },
            { "3", ShowInternetAttachment }
        };

        public async Task StartAsync(IDialogContext context)
        {
            MenuManager = new MenuManager() { FilePath = @"C:\Users\cdavidso\Desktop\parser\menu.pdf" };
            LunchMenu = MenuManager.LunchMenu;

            context.Wait(this.MessageReceivedAsync);
        }

        public async virtual Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            var welcomeMessage = context.MakeMessage();
            welcomeMessage.Text = $"Hey, {context.Activity.Recipient.Name} I'm lunch bot.";

            await context.PostAsync(welcomeMessage);

            //await this.DisplayOptionsAsync(context);
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

        private Attachment GetMenuDocument(object filePath)
        {
            return new Attachment(
                "image/pdf",
                MenuManager.FilePath,
                null,
                "MegaBytes Menu");
        }

        private string GetMenuWeek()
        {
            var fullMenu = "";

            for (int i = 0; i < 5; i++)
            {
                fullMenu += $"{Enum.GetName(typeof(DayOfWeek), i)}:\n{GetDaysMenu(i)}";
            }

            return fullMenu;
        }

        private string GetDaysMenu(int dayOfWeek)
        {
            var today = LunchMenu.Menu[dayOfWeek];

            var stationStrs = today.FoodStations.Select(s => $"{s.FoodName} ${s.Price}\n");

            return string.Join("\n", stationStrs);
        }
    }
}