namespace BlueQ.Bot.Dialogs
{
    using System.Collections.Generic;
    using Autofac;
    using BlueQ.BotAssets;
    using BotAssets.Dialogs;
    using Microsoft.Bot.Builder.Internals.Fibers;

    public class ParticipantLocationDialogFactory : DialogFactory, IParticipantLocationDialogFactory
    {
        public ParticipantLocationDialogFactory(IComponentContext scope)
            : base(scope)
        {
        }

        public SavedParticipantLocationDialog CreateSavedParticipantLocationDialog(
            string prompt,
            string useSavedAddressPrompt,
            string saveAddressPrompt,
            IDictionary<string, string> savedAddresses,
            IEnumerable<string> saveOptionNames)
        {
            return this.Scope.Resolve<SavedParticipantLocationDialog>(
                new NamedParameter("prompt", prompt),
                new NamedParameter("useSavedAddressPrompt", useSavedAddressPrompt),
                new NamedParameter("saveAddressPrompt", saveAddressPrompt),
                TypedParameter.From(savedAddresses),
                TypedParameter.From(saveOptionNames));
        }
    }
}