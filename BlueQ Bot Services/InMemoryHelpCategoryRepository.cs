namespace BlueQ.Bot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using Models;

    public class InMemoryHelpCategoryRepository : InMemoryRepositoryBase<HelpCategory>
    {
        private IEnumerable<HelpCategory> helpCategories;

        public InMemoryHelpCategoryRepository()
        {
            this.helpCategories = Enumerable.Range(1, 5)
                .Select(i => new HelpCategory
                {
                    Name = $"HelpCategory {i}",
                    ImageUrl = $"https://placeholdit.imgix.net/~text?txtsize=48&txt={HttpUtility.UrlEncode("HelpCategory " + i)}&w=640&h=330"
                });
        }

        public override HelpCategory GetByName(string name)
        {
            return this.helpCategories.SingleOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        protected override IEnumerable<HelpCategory> Find(Func<HelpCategory, bool> predicate)
        {
            return predicate != default(Func<HelpCategory, bool>) ? this.helpCategories.Where(predicate) : this.helpCategories;
        }
    }
}