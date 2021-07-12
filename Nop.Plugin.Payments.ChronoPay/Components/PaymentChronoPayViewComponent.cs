using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.ChronoPay.Components
{
    [ViewComponent(Name = "PaymentChronoPay")]
    public class PaymentChronoPayViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.ChronoPay/Views/PaymentInfo.cshtml");
        }
    }
}
