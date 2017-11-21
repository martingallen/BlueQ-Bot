namespace BlueQ.Bot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using System.Web;
    using AutoMapper;
    using BotAssets.Dialogs;
    using BotAssets.Extensions;
    using Microsoft.Bot.Builder.ConnectorEx;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.Location;
    using Microsoft.Bot.Connector;
    using Models;
    using Properties;
    using Services;
    using Services.Models;

    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private readonly string checkoutUriFormat;
        private readonly IParticipantLocationDialogFactory dialogFactory;
        private readonly IHelpdeskTicketService helpdeskTicketService;

        private Models.HelpdeskTicket _helpdeskTicket;
        private ConversationReference conversationReference;
        
        public RootDialog(string checkoutUriFormat, IParticipantLocationDialogFactory dialogFactory, IHelpdeskTicketService helpdeskTicketService)
        {
            this.checkoutUriFormat = checkoutUriFormat;
            this.dialogFactory = dialogFactory;
            this.helpdeskTicketService = helpdeskTicketService;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(this.MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            if (this.conversationReference == null)
            {
                this.conversationReference = message.ToConversationReference();
            }

            await this.WelcomeMessageAsync(context);
        }

        private async Task WelcomeMessageAsync(IDialogContext context)
        {
            var reply = context.MakeMessage();

            var options = new[]
            {
                Resources.RootDialog_Welcome_ITOption,
                Resources.RootDialog_Welcome_FMOption,
                Resources.RootDialog_Welcome_HROption,
                Resources.RootDialog_Welcome_FinanceOption,
                Resources.RootDialog_Welcome_OtherSupport
            };
            reply.AddHeroCard(
                Resources.RootDialog_Welcome_Title,
                Resources.RootDialog_Welcome_Subtitle,
                options,
                new[] { "https://placeholdit.imgix.net/~text?txtsize=56&txt=BlueQ%20Helpdesk&w=640&h=330" });

            await context.PostAsync(reply);

            context.Wait(this.OnOptionSelected);
        }

        private async Task OnOptionSelected(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            if (message.Text == Resources.RootDialog_Welcome_ITOption)
            {
                this._helpdeskTicket = new Models.HelpdeskTicket();

                // BotBuilder's LocationDialog
                // Leverage DI to inject other parameters
                var locationDialog = this.dialogFactory.Create<LocationDialog>(
                    new Dictionary<string, object>()
                    {
                        { "prompt", string.Format(CultureInfo.CurrentCulture, Resources.RootDialog_ParticipantLocation_Prompt, message.From.Name ?? "User") },
                        { "channelId", context.Activity.ChannelId }
                    });

                context.Call(locationDialog, this.AfterLocationDetermined);
            }
            else if (message.Text == Resources.RootDialog_Welcome_HROption)
            {
                await this.StartOverAsync(context, Resources.RootDialog_Support_Message);
            }
            else if (message.Text == Resources.RootDialog_Welcome_FMOption)
            {
                await this.StartOverAsync(context, Resources.RootDialog_Support_Message);
            }
            else if (message.Text == Resources.RootDialog_Welcome_FinanceOption)
            {
                await this.StartOverAsync(context, Resources.RootDialog_Support_Message);
            }
            else if (message.Text == Resources.RootDialog_Welcome_OtherSupport)
            {
                await this.StartOverAsync(context, Resources.RootDialog_Support_Message);
            }
            else
            {
                await this.StartOverAsync(context, Resources.RootDialog_Welcome_Error);
            }
        }

        private async Task AfterLocationDetermined(IDialogContext context, IAwaitable<Place> result)
        {
            try
            {
                var place = await result;
                var formattedAddress = place.GetPostalAddress().FormattedAddress;
                this._helpdeskTicket.Location = formattedAddress;

                context.Call(this.dialogFactory.Create<HelpCategoryDialog>(), this.AfterHelpdeskCategorySelected);
            }
            catch (TooManyAttemptsException)
            {
                await this.StartOverAsync(context, Resources.RootDialog_TooManyAttempts);
            }
        }

        private async Task AfterHelpdeskCategorySelected(IDialogContext context, IAwaitable<string> result)
        {
            this._helpdeskTicket.HelpCategoryName = await result;

            context.Call(this.dialogFactory.Create<HelpItemSelectionDialog, string>(this._helpdeskTicket.HelpCategoryName), this.AfterHelpItemSelected);
        }

        private async Task AfterHelpItemSelected(IDialogContext context, IAwaitable<HelpItem> result)
        {
            var helpItem = await result;

            this._helpdeskTicket.HelpItem = helpItem;

            await context.PostAsync(string.Format(CultureInfo.CurrentCulture, Resources.RootDialog_HelpItem_Selected, this._helpdeskTicket.HelpItem.Title));

            PromptDialog.Choice(context, this.AfterDeliveryDateSelected, new[] { StringConstants.Today, StringConstants.Tomorrow }, Resources.RootDialog_DeliveryDate_Prompt);
        }

        private async Task AfterDeliveryDateSelected(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                this._helpdeskTicket.DeliveryDate = (await result == StringConstants.Today) ? DateTime.Today : DateTime.Today.AddDays(1);

                await context.PostAsync(string.Format(CultureInfo.CurrentCulture, Resources.RootDialog_DeliveryDate_Selected, this._helpdeskTicket.HelpItem.Title, this._helpdeskTicket.DeliveryDate.ToShortDateString()));

                ParticipantPreferences participantPreferences;
                if (context.UserData.TryGetValue(StringConstants.UserPreferencesKey, out participantPreferences))
                {
                    this._helpdeskTicket.ParticipantEmail = participantPreferences.ParticipantEmail;
                    this._helpdeskTicket.ParticipantPhoneNumber = participantPreferences.ParticipantPhoneNumber;

                    this._helpdeskTicket.AskToUseSavedParticipantInfo = !string.IsNullOrWhiteSpace(this._helpdeskTicket.ParticipantEmail) && !string.IsNullOrWhiteSpace(this._helpdeskTicket.ParticipantPhoneNumber);
                }

                var helpdeskTicketForm = new FormDialog<Models.HelpdeskTicket>(this._helpdeskTicket, Models.HelpdeskTicket.BuildHelpdeskTicketForm, FormOptions.PromptInStart);
                context.Call(helpdeskTicketForm, this.AfterHelpdeskTicketForm);
            }
            catch (TooManyAttemptsException)
            {
                await this.StartOverAsync(context, Resources.RootDialog_TooManyAttempts);
            }
        }

        private async Task AfterHelpdeskTicketForm(IDialogContext context, IAwaitable<Models.HelpdeskTicket> result)
        {
            try
            {
                await result;
                if (this._helpdeskTicket.SavedParticipantInfo)
                {
                    context.UserData.UpdateValue<ParticipantPreferences>(
                        StringConstants.UserPreferencesKey,
                        userPreferences =>
                        {
                            userPreferences.ParticipantEmail = this._helpdeskTicket.ParticipantEmail;
                            userPreferences.ParticipantPhoneNumber = this._helpdeskTicket.ParticipantPhoneNumber;
                        });
                }

                var savedAddresses = new Dictionary<string, string>();
                ParticipantPreferences preferences;

                if (context.UserData.TryGetValue(StringConstants.UserPreferencesKey, out preferences))
                {
                    savedAddresses = preferences.ParticipantLocations;
                }

                var LocationDialog = this.dialogFactory.CreateSavedParticipantLocationDialog(
                    Resources.RootDialog_BillingAddress_Prompt,
                    Resources.RootDialog_Location_SelectSaved,
                    Resources.RootDialog_Location_ShouldSave,
                    savedAddresses,
                    new[] { StringConstants.HomeBillingAddress, StringConstants.WorkBillingAddress });

                context.Call(LocationDialog, this.AfterBillingAddress);
            }
            catch (FormCanceledException e)
            {
                string reply;

                if (e.InnerException == null)
                {
                    reply = Resources.RootDialog_HelpdeskTicket_Cancellation;
                }
                else
                {
                    reply = string.Format(CultureInfo.CurrentCulture, Resources.RootDialog_HelpdeskTicket_Error, e.InnerException.Message);
                }

                await this.StartOverAsync(context, reply);
            }
            catch (TooManyAttemptsException)
            {
                await this.StartOverAsync(context, Resources.RootDialog_TooManyAttempts);
            }
        }

        private async Task AfterBillingAddress(IDialogContext context, IAwaitable<SavedParticipantLocationDialog.SavedLocationResult> result)
        {
            try
            {
                var addressResult = await result;
                this._helpdeskTicket.Location = addressResult.Value;

                if (!string.IsNullOrWhiteSpace(addressResult.SaveOptionName))
                {
                    context.UserData.UpdateValue<ParticipantPreferences>(
                        StringConstants.UserPreferencesKey,
                        userPreferences =>
                        {
                            userPreferences.ParticipantLocations = userPreferences.ParticipantLocations ?? new Dictionary<string, string>();
                            userPreferences.ParticipantLocations[addressResult.SaveOptionName.ToLower()] = this._helpdeskTicket.Location;
                        });
                }

                await this.StaffDetailSelectionAsync(context);
            }
            catch (TooManyAttemptsException)
            {
                await this.StartOverAsync(context, Resources.RootDialog_TooManyAttempts);
            }
        }

        private async Task StaffDetailSelectionAsync(IDialogContext context)
        {
            var paymentReply = context.MakeMessage();

            var serviceModel = Mapper.Map<Services.Models.HelpdeskTicket>(this._helpdeskTicket);
            if (this._helpdeskTicket.TicketId == null)
            {
                this._helpdeskTicket.TicketId = this.helpdeskTicketService.SubmitHelpdeskTicket(serviceModel);
            }

            var checkoutUrl = this.BuildTicketConfirmationUrl(this._helpdeskTicket.TicketId);
            paymentReply.Attachments = new List<Attachment>
                {
                    new HeroCard()
                    {
                        Text = string.Format(CultureInfo.CurrentCulture, Resources.RootDialog_ConfirmTicket_Prompt, this._helpdeskTicket.HelpItem.SubTitle),
                        Buttons = new List<CardAction>
                        {
                            new CardAction(ActionTypes.OpenUrl, Resources.RootDialog_ConfirmTicket_Continue, value: checkoutUrl),
                            new CardAction(ActionTypes.ImBack, Resources.RootDialog_ConfirmTicket_Cancel, value: Resources.RootDialog_ConfirmTicket_Cancel)
                        }
                    }.ToAttachment()
                };

            await context.PostAsync(paymentReply);

            context.Wait(this.AfterPaymentSelection);
        }

        private string BuildTicketConfirmationUrl(string helpdeskTicketId)
        {
            var uriBuilder = new UriBuilder(this.checkoutUriFormat);

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["helpdeskTicketId"] = helpdeskTicketId;
            query["botId"] = this.conversationReference.Bot.Id;
            query["channelId"] = this.conversationReference.ChannelId;
            query["conversationId"] = this.conversationReference.Conversation.Id;
            query["serviceUrl"] = this.conversationReference.ServiceUrl;
            query["userId"] = this.conversationReference.User.Id;

            uriBuilder.Query = query.ToString();
            var checkoutUrl = uriBuilder.Uri.ToString();

            return checkoutUrl;
        }

        private async Task AfterPaymentSelection(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var selection = await result;

            if (selection.Text == Resources.RootDialog_ConfirmTicket_Cancel)
            {
                var options = new[] { Resources.RootDialog_Menu_StartOver, Resources.RootDialog_Menu_Cancel, Resources.RootDialog_Welcome_OtherSupport };
                PromptDialog.Choice(context, this.AfterChangedMyMind, options, Resources.RootDialog_Menu_Prompt);
            }
            else
            {
                var helpdeskTicket = this.helpdeskTicketService.RetrieveHelpdeskTicket(selection.Text);
                if (helpdeskTicket == null || !helpdeskTicket.Resolved)
                {
                    await context.PostAsync(string.Format(CultureInfo.CurrentCulture, Resources.RootDialog_ConfirmTicket_Error, selection.Text));
                    await this.StaffDetailSelectionAsync(context);
                    return;
                }

                var message = context.MakeMessage();
                message.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.RootDialog_Receipt_Text,
                   selection.Text,
                   this._helpdeskTicket.HelpItem.Title,
                   this._helpdeskTicket.HelpCategoryName,
                   this._helpdeskTicket.Location,
                   this._helpdeskTicket.Note);
                message.Attachments.Add(this.GetReceiptCard());

                await this.StartOverAsync(context, message);
            }
        }

        private async Task AfterChangedMyMind(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                var option = await result;

                if (option == Resources.RootDialog_Menu_StartOver)
                {
                    await this.StartOverAsync(context, Resources.RootDialog_Welcome_Message);
                }
                else if (option == Resources.RootDialog_Menu_Cancel)
                {
                    await this.StaffDetailSelectionAsync(context);
                }
                else
                {
                    await this.StartOverAsync(context, Resources.RootDialog_Support_Message);
                }
            }
            catch (TooManyAttemptsException)
            {
                await this.StartOverAsync(context, Resources.RootDialog_TooManyAttempts);
            }
        }

        private Attachment GetReceiptCard()
        {
            var helpdeskTicket = this.helpdeskTicketService.RetrieveHelpdeskTicket(this._helpdeskTicket.TicketId);
            var receiptCard = new ReceiptCard
            {
                Title = Resources.RootDialog_Receipt_Title,
                Facts = new List<Fact>
                {
                    new Fact(Resources.RootDialog_Receipt_HelpdeskID, helpdeskTicket.TicketId),
                    new Fact(Resources.RootDialog_Receipt_StaffNumber, helpdeskTicket.StaffDetails.StaffNumber),
                    new Fact(Resources.RootDialog_Receipt_Department, helpdeskTicket.StaffDetails.Department)
                },
                Items = new List<ReceiptItem>
                {
                    new ReceiptItem(
                        title: helpdeskTicket.HelpCategoryName,
                        subtitle: helpdeskTicket.HelpItem.Title,
                        image: new CardImage(helpdeskTicket.HelpItem.ImageUrl)),
                }
            };

            return receiptCard.ToAttachment();
        }

        private async Task StartOverAsync(IDialogContext context, string text)
        {
            var message = context.MakeMessage();
            message.Text = text;
            await this.StartOverAsync(context, message);
        }

        private async Task StartOverAsync(IDialogContext context, IMessageActivity message)
        {
            await context.PostAsync(message);
            this._helpdeskTicket = new Models.HelpdeskTicket();
            await this.WelcomeMessageAsync(context);
        }
    }
}