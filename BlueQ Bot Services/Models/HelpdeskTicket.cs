namespace BlueQ.Bot.Services.Models
{
    using System;

    [Serializable]
    public class HelpdeskTicket
    {
        public string TicketId { get; set; }

        public string Note { get; set; }

        public string ParticipantEmail { get; set; }

        public string ParticipantPhoneNumber { get; set; }

        public bool SavedParticipantInfo { get; set; }

        public string Location { get; set; }

        public string HelpCategoryName { get; set; }

        public HelpItem HelpItem { get; set; }

        public DateTime DeliveryDate { get; set; }

        public bool Resolved { get; set; }

        public StaffDetails StaffDetails { get; set; }
    }
}
