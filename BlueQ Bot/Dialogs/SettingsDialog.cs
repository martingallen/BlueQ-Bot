namespace BlueQ.Bot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using BotAssets;
    using BotAssets.Dialogs;
    using BotAssets.Extensions;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Internals.Fibers;
    using Microsoft.Bot.Builder.Location;
    using Microsoft.Bot.Connector;
    using Models;
    using Properties;

    [Serializable]
    public class SettingsDialog : IDialog<object>
    {
        private readonly IParticipantLocationDialogFactory dialogFactory;

        private string selectedAddressToUpdate;

        public SettingsDialog(IParticipantLocationDialogFactory dialogFactory)
        {
            SetField.NotNull(out this.dialogFactory, nameof(dialogFactory), dialogFactory);
        }

        public async Task StartAsync(IDialogContext context)
        {
            this.selectedAddressToUpdate = null;

            var preferencesOptions = new[]
                 {
                    Resources.SettingsDialog_Edit_Email,
                    Resources.SettingsDialog_Edit_PhoneNumber,
                    Resources.SettingsDialog_Edit_Location
                };
            CancelablePromptChoice<string>.Choice(
                context,
                this.ResumeAfterOptionSelected,
                preferencesOptions,
                Resources.SettingsDialog_Prompt);
        }

        public async virtual Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            context.Wait(this.MessageReceivedAsync);
        }

        private static ParticipantPreferences GetUserPreferences(IDialogContext context)
        {
            ParticipantPreferences participantPreferences = null;

            context.UserData.TryGetValue(StringConstants.UserPreferencesKey, out participantPreferences);

            return participantPreferences;
        }

        private async Task ResumeAfterOptionSelected(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                var option = await result;

                if (option == null)
                {
                    context.Done<object>(null);
                    return;
                }

                var userPreferences = GetUserPreferences(context);
                switch (option.Substring(0, 1))
                {
                    case "1":
                        this.PromptString(context, "email", userPreferences != null ? userPreferences.ParticipantEmail : null, RegexConstants.Email, this.ResumeAfterEmailEntered);
                        break;

                    case "2":
                        this.PromptString(context, "phone number", userPreferences != null ? userPreferences.ParticipantPhoneNumber : null, RegexConstants.Phone, this.ResumeAfterPhoneNumberEntered);
                        break;

                    case "3":
                        await this.PromptAddress(context, userPreferences);
                        break;
                }
            }
            catch (TooManyAttemptsException)
            {
                await this.StartAsync(context);
            }
        }

        private async Task PromptAddress(IDialogContext context, ParticipantPreferences participantPreferences)
        {
            string homeAddress = null;
            string workAddress = null;

            participantPreferences?.ParticipantLocations?.TryGetValue(StringConstants.HomeBillingAddress.ToLower(), out homeAddress);
            participantPreferences?.ParticipantLocations?.TryGetValue(StringConstants.WorkBillingAddress.ToLower(), out workAddress);

            var message = context.MakeMessage();
            message.Text = Resources.SettingsDialog_PromptLocation_Ask;
            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            message.AddHeroCard(StringConstants.HomeBillingAddress, homeAddress ?? StringConstants.NotSetBillingAddress, new[] { StringConstants.HomeBillingAddress });
            message.AddHeroCard(StringConstants.WorkBillingAddress, workAddress ?? StringConstants.NotSetBillingAddress, new[] { StringConstants.WorkBillingAddress });
            message.AddHeroCard("Back", "Go back", new[] { "Back" });

            await context.PostAsync(message);
            context.Wait(this.OnAddressSelectedReceived);
        }

        private async Task OnAddressSelectedReceived(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            if (message.Text.Equals(StringConstants.HomeBillingAddress, StringComparison.InvariantCultureIgnoreCase)
                || message.Text.Equals(StringConstants.WorkBillingAddress, StringComparison.InvariantCultureIgnoreCase))
            {
                this.selectedAddressToUpdate = message.Text;

                // BotBuilder's LocationDialog
                // Leverage DI to inject other parameters
                var locationDialog = this.dialogFactory.Create<LocationDialog>(
                    new Dictionary<string, object>()
                    {
                        { "prompt", Resources.SettingsDialog_Location_Prompt },
                        { "channelId", context.Activity.ChannelId }
                    });

                context.Call(locationDialog, this.ResumeAfterAddressEntered);
            }
            else if (message.Text.Equals("B", StringComparison.InvariantCultureIgnoreCase) || message.Text.Equals("Back", StringComparison.InvariantCultureIgnoreCase))
            {
                await this.StartAsync(context);
            }
            else
            {
                await this.PromptAddress(context, GetUserPreferences(context));
            }
        }

        private async Task ResumeAfterAddressEntered(IDialogContext context, IAwaitable<Place> result)
        {
            string reply;

            var place = await result;
            if (place == null)
            {
                reply = "No address was selected, returning to settings menu.";
            }
            else
            {
                var formattedAddress = place.GetPostalAddress().FormattedAddress;

                context.UserData.UpdateValue<ParticipantPreferences>(
                        StringConstants.UserPreferencesKey,
                        userPreferences =>
                        {
                            userPreferences.ParticipantLocations = userPreferences.ParticipantLocations ?? new Dictionary<string, string>();
                            userPreferences.ParticipantLocations[this.selectedAddressToUpdate.ToLower()] = formattedAddress;
                        });

                reply = string.Format(CultureInfo.CurrentCulture, Resources.SettingsDialog_Location_Entered, this.selectedAddressToUpdate, formattedAddress);
            }

            await context.PostAsync(reply);
            await this.StartAsync(context);
        }

        private void PromptString(IDialogContext context, string ask, string currentValue, string regexPattern, ResumeAfter<string> resume)
        {
            string prompt = string.Format(CultureInfo.CurrentCulture, Resources.SettingsDialog_PromptString, ask);

            if (!string.IsNullOrEmpty(currentValue))
            {
                prompt = string.Format(CultureInfo.CurrentCulture, Resources.SettingsDialog_PromptString_CurrentValue, ask, currentValue);
            }

            string retry = string.Format(CultureInfo.CurrentCulture, Resources.SettingsDialog_PromptString_Retry, ask);

            var promptString = new PromptStringRegex(prompt, regexPattern, retry);
            context.Call(promptString, resume);
        }

        private async Task ResumeAfterEmailEntered(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                var email = await result;

                if (!string.IsNullOrEmpty(email))
                {
                    context.UserData.UpdateValue<ParticipantPreferences>(StringConstants.UserPreferencesKey, (u) => u.ParticipantEmail = email);
                    await context.PostAsync(string.Format(CultureInfo.CurrentCulture, Resources.SettingsDialog_Email_Entered, email));
                }
            }
            finally
            {
                await this.StartAsync(context);
            }
        }

        private async Task ResumeAfterPhoneNumberEntered(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                var phone = await result;

                if (!string.IsNullOrEmpty(phone))
                {
                    context.UserData.UpdateValue<ParticipantPreferences>(StringConstants.UserPreferencesKey, (u) => u.ParticipantPhoneNumber = phone);
                    await context.PostAsync(string.Format(CultureInfo.CurrentCulture, Resources.SettingsDialog_PhoneNumber_Entered, phone));
                }
            }
            finally
            {
                await this.StartAsync(context);
            }
        }
    }
}