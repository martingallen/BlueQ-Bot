



namespace BlueQ.Bot.Controllers
{
    using System;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using Microsoft.Bot.Builder.ConnectorEx;
    using Microsoft.Bot.Builder.Dialogs;
    using BlueQ.Bot.Services;
    using BlueQ.Bot.Services.Models;

    [RoutePrefix("CheckOut")]
    [RequireHttps]
    public class CheckOutController : Controller
    {
        private readonly IHelpdeskTicketService helpdeskTicketService;

        public CheckOutController(IHelpdeskTicketService helpdeskTicketService)
        {
            this.helpdeskTicketService = helpdeskTicketService;
        }

        [Route("")]
        [HttpGet]
        public ActionResult Index(string state, string helpdeskTicketId)
        {
            var helpdeskTicket = this.helpdeskTicketService.RetrieveHelpdeskTicket(helpdeskTicketId);

            // Check ticket exists
            if (helpdeskTicket == null)
            {
                throw new ArgumentException("Helpdesk Ticket ID not found", "helpdeskTicketId");
            }

            // Check ticket to see if is already processed
            if (helpdeskTicket.Resolved)
            {
                return this.RedirectToAction("Completed", new { helpdeskTicketId });
            }

            // Payment form
            this.ViewBag.State = state;
            return this.View(helpdeskTicket);
        }

        [Route("")]
        [HttpPost]
        public async Task<ActionResult> Index(
            string botId,
            string channelId,
            string conversationId,
            string serviceUrl,
            string userId,
            string helpdeskTicketId,
            StaffDetails staffDetails)

        {
            this.helpdeskTicketService.ConfirmHelpdeskTicket(helpdeskTicketId, staffDetails);

            var address = new Address(botId, channelId, userId, conversationId, serviceUrl);
            var conversationReference = address.ToConversationReference();
            var message = conversationReference.GetPostToBotMessage();

            message.Text = helpdeskTicketId;

            await Conversation.ResumeAsync(conversationReference, message);

            return this.RedirectToAction("Completed", new { helpdeskTicketId });
        }

        [Route("completed")]
        public ActionResult Completed(string helpdeskTicketId)
        {
            var helpdeskTicket = this.helpdeskTicketService.RetrieveHelpdeskTicket(helpdeskTicketId);
            if (helpdeskTicketId == null)
            {
                throw new ArgumentException("Helpdesk Ticket ID not found", "helpdeskTicketId");
            }

            return this.View("Completed", helpdeskTicket);
        }
    }
}