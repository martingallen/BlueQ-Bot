namespace BlueQ.Bot.Services
{
    using Models;

    public interface IHelpdeskTicketService
    {
        string SubmitHelpdeskTicket(HelpdeskTicket helpdeskTicket);

        HelpdeskTicket RetrieveHelpdeskTicket(string helpdeskTicketId);

        void ConfirmHelpdeskTicket(string helpdeskTicketId, StaffDetails staffDetails);
    }
}
