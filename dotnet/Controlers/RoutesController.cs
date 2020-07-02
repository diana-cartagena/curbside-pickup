namespace service.Controllers
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using StorePickup.Data;
    using StorePickup.Models;
    using StorePickup.Services;
    using Vtex.Api.Context;

    public class RoutesController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IIOServiceContext _context;
        private readonly IStorePickupService _storePickupService;

        public RoutesController(IHttpContextAccessor httpContextAccessor, IIOServiceContext context, IStorePickupService storePickupService)
        {
            this._httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._storePickupService = storePickupService ?? throw new ArgumentNullException(nameof(storePickupService));
        }

        public async Task<IActionResult> CreateHook()
        {
            HookNotification createOrUpdateHookResponse = await this._storePickupService.CreateOrUpdateHook();
            Response.Headers.Add("Cache-Control", "private");

            return Json(createOrUpdateHookResponse);
        }

        public async Task<IActionResult> CreateDefaultTemplates()
        {
            //bool atLocation = await this._storePickupService.CreateDefaultTemplate(StorePickUpConstants.MailTemplateType.AtLocation);
            //bool packageReady = await this._storePickupService.CreateDefaultTemplate(StorePickUpConstants.MailTemplateType.PackageReady);
            ////bool pickedUp = await this._storePickupService.CreateDefaultTemplate(StorePickUpConstants.MailTemplateType.PickedUp);
            //bool readyForPacking = await this._storePickupService.CreateDefaultTemplate(StorePickUpConstants.MailTemplateType.ReadyForPacking);
            //Response.Headers.Add("Cache-Control", "private");

            //return Json($"AtLocation:{atLocation} PackageReady:{packageReady} ReadyForPacking:{readyForPacking}");
            return Json($"N/A");
        }

        public async Task<IActionResult> ProcessNotification()
        {
            bool success = false;
            ActionResult status = BadRequest();
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                Console.WriteLine($"[Hook Notification] : '{bodyAsText}'");
                dynamic notification = JsonConvert.DeserializeObject<dynamic>(bodyAsText);
                if (notification != null && notification.hookConfig != null && notification.hookConfig == StorePickUpConstants.HookPing)
                {
                    status = Ok();
                    success = true;
                }
                else
                {
                    HookNotification hookNotification = JsonConvert.DeserializeObject<HookNotification>(bodyAsText);
                    success = await _storePickupService.ProcessNotification(hookNotification);
                    status = success ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
                }

                _context.Vtex.Logger.Info("ProcessNotification", null, $"Success? [{success}] for {bodyAsText}");
            }
            else
            {
                Console.WriteLine($"[Hook Notification] : '{HttpContext.Request.Method}'");
            }

            Console.WriteLine($"[Process Notification] : '{success}'");
            return status;
        }

        public async Task<IActionResult> ProcessMailLink(string linkAction, string arguments)
        {
            string redirectUrl = "~/";
            //arguments = HttpUtility.HtmlDecode(arguments);
            arguments = Uri.UnescapeDataString(arguments);
            Console.WriteLine($"ProcessLink {linkAction} on {arguments}");
            redirectUrl = await _storePickupService.ProcessLink(linkAction, arguments);
            Console.WriteLine($"redirectUrl = {redirectUrl}");
            return Redirect(redirectUrl);
        }
    }
}
