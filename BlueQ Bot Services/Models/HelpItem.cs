namespace BlueQ.Bot.Services.Models
{
    using System;

    [Serializable]
    public class HelpItem
    {
        public string Title { get; set; }

        public string SubTitle { get; set; }

        public string ImageUrl { get; set; }

        public string HelpCategory { get; set; }
    }
}