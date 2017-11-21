namespace BlueQ.Bot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;

    public class InMemoryHelpdeskTicketService : IHelpdeskTicketService
    {
        private IList<HelpdeskTicket> helpDeskTickets;

        public InMemoryHelpdeskTicketService()
        {
            this.helpDeskTickets = new List<HelpdeskTicket>();
        }

        public void ConfirmHelpdeskTicket(string helpdeskTicketId, StaffDetails staffDetails)
        {
            var helpdeskTicket = this.RetrieveHelpdeskTicket(helpdeskTicketId);
            if (helpdeskTicket == null)
            {
                throw new InvalidOperationException("Helpdesk Ticket ID not found.");
            }

            if (helpdeskTicket.Resolved)
            {
                throw new InvalidOperationException("Helpdesk Ticket already resolved.");
            }

            helpdeskTicket.Resolved = true;
            helpdeskTicket.StaffDetails = staffDetails;
        }

        public string SubmitHelpdeskTicket(HelpdeskTicket helpdeskTicket)
        {
            helpdeskTicket.TicketId = Guid.NewGuid().ToString();
            helpdeskTicket.Resolved = false;
            this.helpDeskTickets.Add(helpdeskTicket);

            return helpdeskTicket.TicketId;
        }

        public HelpdeskTicket RetrieveHelpdeskTicket(string helpdeskTicketId)
        {
            return this.helpDeskTickets.FirstOrDefault(o => o.TicketId == helpdeskTicketId);
        }
    }
}