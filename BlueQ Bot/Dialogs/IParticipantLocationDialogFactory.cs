namespace BlueQ.Bot.Dialogs
{
    using System.Collections.Generic;
    using BlueQ.BotAssets;
    using BlueQ.BotAssets.Dialogs;

    public interface IParticipantLocationDialogFactory : IDialogFactory
    {
        SavedParticipantLocationDialog CreateSavedParticipantLocationDialog(
            string prompt,
            string useSavedAddressPrompt,
            string saveAddressPrompt,
            IDictionary<string, string> savedAddresses,
            IEnumerable<string> saveOptionNames);
    }
}