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
    public class HelpCategoryDialog : PagedCarouselDialog<string>
    {
        private readonly IRepository<HelpCategory> repository;

        public HelpCategoryDialog(IRepository<HelpCategory> repository)
        {
            this.repository = repository;
        }

        public override string Prompt
        {
            get { return Resources.HelpCategoryDialog_Prompt; }
        }

        public override PagedCarouselCards GetCarouselCards(int pageNumber, int pageSize)
        {
            var pagedResult = this.repository.RetrievePage(pageNumber, pageSize);

            var carouselCards = pagedResult.Items.Select(it => new HeroCard
            {
                Title = it.Name,
                Images = new List<CardImage> { new CardImage(it.ImageUrl, it.Name) },
                Buttons = new List<CardAction> { new CardAction(ActionTypes.ImBack, Resources.HelpCategoryDialog_Select, value: it.Name) }
            });

            return new PagedCarouselCards
            {
                Cards = carouselCards,
                TotalCount = pagedResult.TotalCount
            };
        }

        public override async Task ProcessMessageReceived(IDialogContext context, string helpCategoryName)
        {
            var HelpCategory = this.repository.GetByName(helpCategoryName);

            if (HelpCategory != null)
            {
                context.Done(HelpCategory.Name);
            }
            else
            {
                await context.PostAsync(string.Format(CultureInfo.CurrentCulture, Resources.HelpCategoryDialog_InvalidOption, helpCategoryName));
                await this.ShowItems(context);
                context.Wait(this.MessageReceivedAsync);
            }
        }
    }
}