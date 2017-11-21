namespace BlueQ.BotAssets.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Location;
    using Microsoft.Bot.Connector;
    using Properties;

    [Serializable]
    public class SavedParticipantLocationDialog : IDialog<SavedParticipantLocationDialog.SavedLocationResult>
    {
        private readonly IDictionary<string, string> savedLocations;
        private readonly IEnumerable<string> saveOptionNames;
        private readonly string prompt;
        private readonly string useSavedLocationPrompt;
        private readonly string saveLocationPrompt;
        private readonly IDialogFactory dialogFactory;

        private string currentAddress;

        public SavedParticipantLocationDialog(
            string prompt,
            string useSavedLocationPrompt,
            string saveLocationPrompt,
            IDictionary<string, string> savedLocations,
            IEnumerable<string> saveOptionNames,
            IDialogFactory dialogFactory)
        {
            this.savedLocations = savedLocations ?? new Dictionary<string, string>();
            this.saveOptionNames = saveOptionNames;
            this.prompt = prompt;
            this.useSavedLocationPrompt = useSavedLocationPrompt;
            this.saveLocationPrompt = saveLocationPrompt;
            this.dialogFactory = dialogFactory;
        }

        public async Task StartAsync(IDialogContext context)
        {
            if (this.savedLocations.Any())
            {
                PromptDialog.Choice(context, this.AfterSelectSavedLocation, this.savedLocations.Values.Concat(new[] { Resources.SavedLocationDialog_AddNewLocation }), this.useSavedLocationPrompt);
            }
            else
            {
                this.LocationPrompt(context);
            }
        }

        private void LocationPrompt(IDialogContext context)
        {
            // BotBuilder's LocationDialog
            // Leverage DI to inject other parameters
            var locationDialog = this.dialogFactory.Create<LocationDialog>(
                new Dictionary<string, object>()
                {
                        { "prompt", this.prompt },
                        { "channelId", context.Activity.ChannelId }
                });

            context.Call(locationDialog, this.AfterLocationPrompt);
        }

        private async Task AfterLocationPrompt(IDialogContext context, IAwaitable<Place> result)
        {
            var place = await result;
            this.currentAddress = place.GetPostalAddress().FormattedAddress;
            PromptDialog.Choice(context, this.AfterSelectToSaveLocation, this.saveOptionNames.Concat(new[] { Resources.SavedLocationDialog_NotThisTime }), this.saveLocationPrompt);
        }

        private async Task AfterSelectToSaveLocation(IDialogContext context, IAwaitable<string> result)
        {
            var saveOptionName = await result;
            saveOptionName = saveOptionName == Resources.SavedLocationDialog_NotThisTime ? null : saveOptionName;
            context.Done(new SavedLocationResult { Value = this.currentAddress, SaveOptionName = saveOptionName });
        }

        private async Task AfterSelectSavedLocation(IDialogContext context, IAwaitable<string> result)
        {
            this.currentAddress = await result;
            if (this.currentAddress == Resources.SavedLocationDialog_AddNewLocation)
            {
                this.LocationPrompt(context);
            }
            else
            {
                context.Done(new SavedLocationResult { Value = this.currentAddress });
            }
        }

        public class SavedLocationResult
        {
            public string Value { get; set; }

            public string SaveOptionName { get; set; }
        }
    }
}