namespace BlueQ.Bot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using Models;

    public class InMemoryHelpItemRepository : InMemoryRepositoryBase<HelpItem>
    {
        private IEnumerable<HelpItem> helpItems;

        public InMemoryHelpItemRepository()
        {
            this.helpItems = Enumerable.Range(1, 50)
                .Select(i => new HelpItem
                {
                    Title = $"HelpItem {i}\u2122",
                    ImageUrl = $"https://placeholdit.imgix.net/~text?txtsize=48&txt={HttpUtility.UrlEncode("HelpItem " + i)}&w=640&h=330",
                    SubTitle = $"HelpItem {i}\u2122",
                    
                    // randomizing the helpdesk category but ensuring at least 1 HelpItem for each of it.
                    HelpCategory = (i <= 5) ? $"HelpCategory {i}" : "HelpCategory " + new Random(i).Next(1, 5) 
                });
        }

        public override HelpItem GetByName(string name)
        {
            return this.helpItems.SingleOrDefault(x => x.Title.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        protected override IEnumerable<HelpItem> Find(Func<HelpItem, bool> predicate)
        {
            return predicate != default(Func<HelpItem, bool>) ? this.helpItems.Where(predicate) : this.helpItems;
        }
    }
}