namespace BlueQ.BotAssets
{
    using System;
    using Microsoft.Bot.Builder.Location;
    using Properties;

    [Serializable]
    public class LocationResourceManager : Microsoft.Bot.Builder.Location.LocationResourceManager
    {
        public override string ConfirmationAsk => Resources.LocationDialog_ConfirmationAsk;
    }
}
