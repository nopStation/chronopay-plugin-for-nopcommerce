using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.ChronoPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.ChronoPay.Controllers
{
    public class PaymentChronoPayController : BasePaymentController
    {
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly ChronoPayPaymentSettings _chronoPayPaymentSettings;
        private readonly INotificationService _notificationService;
        private readonly IPaymentPluginManager _paymentPluginManager;

        public PaymentChronoPayController(ILocalizationService localizationService,
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService,
            IPermissionService permissionService,
            ISettingService settingService, 
            ChronoPayPaymentSettings chronoPayPaymentSettings,
            INotificationService notificationService,
            IPaymentPluginManager paymentPluginManager)
        {
            _localizationService = localizationService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _permissionService = permissionService;
            _settingService = settingService;
            _chronoPayPaymentSettings = chronoPayPaymentSettings;
            _notificationService = notificationService;
            _paymentPluginManager = paymentPluginManager;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var model = new ConfigurationModel
            {
                GatewayUrl = _chronoPayPaymentSettings.GatewayUrl,
                ProductId = _chronoPayPaymentSettings.ProductId,
                ProductName = _chronoPayPaymentSettings.ProductName,
                SharedSecrect = _chronoPayPaymentSettings.SharedSecrect,
                AdditionalFee = _chronoPayPaymentSettings.AdditionalFee
            };

            return View("~/Plugins/Payments.ChronoPay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //save settings
            _chronoPayPaymentSettings.GatewayUrl = model.GatewayUrl;
            _chronoPayPaymentSettings.ProductId = model.ProductId;
            _chronoPayPaymentSettings.ProductName = model.ProductName;
            _chronoPayPaymentSettings.SharedSecrect = model.SharedSecrect;
            _chronoPayPaymentSettings.AdditionalFee = model.AdditionalFee;
            await _settingService.SaveSettingAsync(_chronoPayPaymentSettings);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return RedirectToAction("Configure");
        }

        public async Task<IActionResult> IPNHandler(IFormCollection form)
        {
            var processor = await _paymentPluginManager.LoadPluginBySystemNameAsync("Payments.ChronoPay") as ChronoPayPaymentProcessor;
            if (processor == null || !_paymentPluginManager.IsPluginActive(processor) || !processor.PluginDescriptor.Installed)
                throw new NopException("ChronoPay module cannot be loaded");

            if (HostedPaymentHelper.ValidateResponseSign(form, _chronoPayPaymentSettings.SharedSecrect) && int.TryParse(form["cs1"], out int orderId))
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order != null && _orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    await _orderProcessingService.MarkOrderAsPaidAsync(order);
                }
            }

            return RedirectToRoute("Homepage");
        }
    }
}