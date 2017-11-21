using Microsoft.Bot.Connector.Payments;

namespace BlueQ.Bot.Models
{
    using System;
    using BlueQ.BotAssets;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.FormFlow.Advanced;
    using BlueQ.Bot.Services.Models;

    [Serializable]
    public class HelpdeskTicket
    {
        public enum UseSaveInfoResponse
        {
            Yes,
            Edit
        }

        public string TicketId { get; set; }

        [Prompt]
        [Pattern(@"^.{1,200}$")]
        public string Note { get; set; }

        [Prompt]
        [Pattern(RegexConstants.Email)]
        public string ParticipantEmail { get; set; }

        [Prompt]
        [Pattern(RegexConstants.Phone)]
        public string ParticipantPhoneNumber { get; set; }

        public bool AskToUseSavedParticipantInfo { get; set; }

        [Prompt]
        public UseSaveInfoResponse? UseSavedParticipantInfo { get; set; }

        [Prompt]
        public bool SavedParticipantInfo { get; set; }

        public string HelpCategoryName { get; set; }

        public HelpItem HelpItem { get; set; }

        public DateTime DeliveryDate { get; set; }

        public string Location { get; set; }

        public bool Resovled { get; set; }

        public StaffDetails StaffDetails { get; set; }

        public static IForm<HelpdeskTicket> BuildHelpdeskTicketForm()
        {
            return new FormBuilder<HelpdeskTicket>()
                .Field(nameof(Note))
                .Field(new FieldReflector<HelpdeskTicket>(nameof(UseSavedParticipantInfo))
                    .SetActive(state => state.AskToUseSavedParticipantInfo)
                    .SetNext((value, state) =>
                    {
                        var selection = (UseSaveInfoResponse)value;

                        if (selection == UseSaveInfoResponse.Edit)
                        {
                            state.ParticipantEmail = null;
                            state.ParticipantPhoneNumber = null;
                            return new NextStep(new[] { nameof(ParticipantEmail) });
                        }
                        else
                        {
                            return new NextStep();
                        }
                    }))
                .Field(new FieldReflector<HelpdeskTicket>(nameof(ParticipantEmail))
                    .SetActive(state => !state.UseSavedParticipantInfo.HasValue || state.UseSavedParticipantInfo.Value == UseSaveInfoResponse.Edit)
                    .SetNext(
                        (value, state) => (state.UseSavedParticipantInfo == UseSaveInfoResponse.Edit)
                        ? new NextStep(new[] { nameof(ParticipantPhoneNumber) })
                        : new NextStep()))
                .Field(nameof(ParticipantPhoneNumber), state => !state.UseSavedParticipantInfo.HasValue || state.UseSavedParticipantInfo.Value == UseSaveInfoResponse.Edit)
                .Field(nameof(SavedParticipantInfo), state => !state.UseSavedParticipantInfo.HasValue || state.UseSavedParticipantInfo.Value == UseSaveInfoResponse.Edit)
                .Build();
        }
    }
}