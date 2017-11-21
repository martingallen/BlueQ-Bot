namespace BlueQ.Bot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using BotAssets.Dialogs;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Properties;
    using Services;
    using Services.Models;

    [Serializable]
    public class HelpItemSelectionDialog : PagedCarouselDialog<HelpItem>
    {
        private readonly string helpCategory;

        private readonly IRepository<HelpItem> repository;

        public HelpItemSelectionDialog(string helpCategory, IRepository<HelpItem> repository)
        {
            this.helpCategory = helpCategory;
            this.repository = repository;
        }

        public override string Prompt
        {
            get { return string.Format(CultureInfo.CurrentCulture, Resources.HelpItemDialog_Prompt, this.helpCategory); }
        }

        public override PagedCarouselCards GetCarouselCards(int pageNumber, int pageSize)
        {
            var pagedResult = this.repository.RetrievePage(pageNumber, pageSize, (helpItem) => helpItem.HelpCategory == this.helpCategory);

            var carouselCards = pagedResult.Items.Select(it => new HeroCard
            {
                Title = it.Title,
                Subtitle = it.SubTitle,
                Images = new List<CardImage> { new CardImage(it.ImageUrl, it.Title) },
                Buttons = new List<CardAction> { new CardAction(ActionTypes.ImBack, Resources.HelpItemDialog_Select, value: it.Title) }
            });

            return new PagedCarouselCards
            {
                Cards = carouselCards,
                TotalCount = pagedResult.TotalCount
            };
        }

        public override async Task ProcessMessageReceived(IDialogContext context, string helpCategoryName)
        {
            var helpItem = this.repository.GetByName(helpCategoryName);

            if (helpItem != null)
            {
                context.Done(helpItem);
            }
            else
            {
                await context.PostAsync(string.Format(CultureInfo.CurrentCulture, Resources.HelpItemDialog_InvalidOption, helpCategoryName));
                await this.ShowItems(context);
                context.Wait(this.MessageReceivedAsync);
            }
        }
    }
}