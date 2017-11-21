namespace BlueQ.BotAssets.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Properties;

    [Serializable]
    public abstract class PagedCarouselDialog<T> : IDialog<T>
    {
        private int pageNumber = 1;
        private int pageSize = 5;

        public virtual string Prompt { get; }

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync(this.Prompt ?? Resources.PagedCarouselDialog_DefaultPrompt);

            await this.ShowItems(context);

            context.Wait(this.MessageReceivedAsync);
        }

        public abstract PagedCarouselCards GetCarouselCards(int pageNumber, int pageSize);

        public abstract Task ProcessMessageReceived(IDialogContext context, string helpCategoryName);

        protected async Task ShowItems(IDialogContext context)
        {
            var reply = context.MakeMessage();
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            reply.Attachments = new List<Attachment>();

            var itemsResult = this.GetCarouselCards(this.pageNumber, this.pageSize);
            foreach (HeroCard productCard in itemsResult.Cards)
            {
                reply.Attachments.Add(productCard.ToAttachment());
            }

            await context.PostAsync(reply);

            if (itemsResult.TotalCount > this.pageNumber * this.pageSize)
            {
                await this.ShowMoreOptions(context);
            }
        }

        protected async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            // TODO: validation
            if (message.Text.Equals(Resources.PagedCarouselDialog_ShowMe, StringComparison.InvariantCultureIgnoreCase))
            {
                this.pageNumber++;
                await this.StartAsync(context);
            }
            else
            {
                await this.ProcessMessageReceived(context, message.Text);
            }
        }

        private async Task ShowMoreOptions(IDialogContext context)
        {
            var moreOptionsReply = context.MakeMessage();
            moreOptionsReply.Attachments = new List<Attachment>
            {
                new HeroCard()
                {
                    Text = Resources.PagedCarouselDialog_MoreOptions,
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, Resources.PagedCarouselDialog_ShowMe, value: Resources.PagedCarouselDialog_ShowMe)
                    }
                }.ToAttachment()
            };

            await context.PostAsync(moreOptionsReply);
        }

        public class PagedCarouselCards
        {
            public IEnumerable<HeroCard> Cards { get; set; }

            public int TotalCount { get; set; }
        }
    }

}