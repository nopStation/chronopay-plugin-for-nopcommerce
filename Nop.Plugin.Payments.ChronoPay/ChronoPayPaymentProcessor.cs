using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework;
using System.Threading.Tasks;
using Nop.Services.Common;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.ChronoPay
{
    /// <summary>
    /// ChronoPay payment processor
    /// </summary>
    public class ChronoPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly ChronoPayPaymentSettings _chronoPayPaymentSettings;
        private readonly ICurrencyService _currencyService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly CurrencySettings _currencySettings;
        private readonly IAddressService _addressService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        #endregion

        #region Ctor

        public ChronoPayPaymentProcessor(ChronoPayPaymentSettings chronoPayPaymentSettings,
            ICurrencyService currencyService, ILocalizationService localizationService,
            ISettingService settingService, IWebHelper webHelper,
            CurrencySettings currencySettings,
            IAddressService addressService,
            IStateProvinceService stateProvinceService,
            ICountryService countryService,
            IHttpContextAccessor httpContextAccessor)
        {
            _chronoPayPaymentSettings = chronoPayPaymentSettings;
            _currencyService = currencyService;
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
            _currencySettings = currencySettings;
            _addressService = addressService;
            _stateProvinceService = stateProvinceService;
            _countryService = countryService;
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
            return Task.FromResult(result);
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var gatewayUrl = new Uri(_chronoPayPaymentSettings.GatewayUrl);

            var post = new RemotePost(_httpContextAccessor,_webHelper)
            {
                FormName = "ChronoPay",
                Url = gatewayUrl.ToString(),
                Method = "POST"
            };

            post.Add("product_id", _chronoPayPaymentSettings.ProductId);
            post.Add("product_name", _chronoPayPaymentSettings.ProductName);
            post.Add("product_price", string.Format(CultureInfo.InvariantCulture, "{0:0.00}", postProcessPaymentRequest.Order.OrderTotal));
            post.Add("product_price_currency", (await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId)).CurrencyCode);
            post.Add("cb_url", $"{_webHelper.GetStoreLocation()}Plugins/PaymentChronoPay/IPNHandler");
            post.Add("cb_type", "P");
            post.Add("cs1", postProcessPaymentRequest.Order.Id.ToString());

            var billingAddress = await _addressService.GetAddressByIdAsync(postProcessPaymentRequest.Order.BillingAddressId);
            post.Add("f_name", billingAddress.FirstName);
            post.Add("s_name", billingAddress.LastName);
            post.Add("street", billingAddress.Address1);
            post.Add("city", billingAddress.City);
            post.Add("zip", billingAddress.ZipPostalCode);
            post.Add("phone", billingAddress.PhoneNumber);
            post.Add("email", billingAddress.Email);

            var state = await _stateProvinceService.GetStateProvinceByIdAsync(billingAddress.StateProvinceId ?? 0);
            if (state != null)
            {
                post.Add("state", state.Abbreviation);
            }

            var country = await _countryService.GetCountryByIdAsync(billingAddress.CountryId ?? 0);
            if (country != null)
            {
                post.Add("country", country.ThreeLetterIsoCode);
            }

            post.Add("sign", HostedPaymentHelper.CalcRequestSign(post.Params, _chronoPayPaymentSettings.SharedSecrect));

            post.Post();
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(_chronoPayPaymentSettings.AdditionalFee);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public  Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //ChronoPay is the redirection payment method
            //It also validates whether order is also paid (after redirection) so customers will not be able to pay twice

            //payment status should be Pending
            if (order.PaymentStatus != PaymentStatus.Pending)
                return Task.FromResult(false);

            //let's ensure that at least 1 minute passed after order is placed
            return Task.FromResult(!((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1));
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentChronoPay/Configure";
        }

        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            return Task.FromResult(new ProcessPaymentRequest());
        }

        public string GetPublicViewComponentName()
        {
            return "PaymentChronoPay";
        }

        public override async Task InstallAsync()
        {
            var settings = new ChronoPayPaymentSettings
            {
                GatewayUrl = "https://secure.chronopay.com/index_shop.cgi",
                ProductId = "",
                ProductName = "",
                SharedSecrect = "",
                AdditionalFee = 0,
            };
            await _settingService.SaveSettingAsync(settings);

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.RedirectionTip", "You will be redirected to ChronoPay site to complete the order.");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.GatewayUrl", "Gateway URL");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.GatewayUrl.Hint", "Enter gateway URL.");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.ProductId", "Product ID");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.ProductId.Hint", "Enter product ID.");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.ProductName", "Product Name");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.ProductName.Hint", "Enter product Name.");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.SharedSecrect", "Shared secret");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.SharedSecrect.Hint", "Enter shared secret.");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.AdditionalFee", "Additional fee");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.ChronoPay.PaymentMethodDescription", "You will be redirected to ChronoPay site to complete the order.");

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {

            await _settingService.DeleteSettingAsync<ChronoPayPaymentSettings>();

            //locales
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.RedirectionTip");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.GatewayUrl");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.GatewayUrl.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.ProductId");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.ProductId.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.ProductName");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.ProductName.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.SharedSecrect");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.SharedSecrect.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.AdditionalFee");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.AdditionalFee.Hint");
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Payments.ChronoPay.PaymentMethodDescription");

            await base.UninstallAsync();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.ChronoPay.PaymentMethodDescription");
        }

        #endregion
    }
}