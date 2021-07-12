using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.ChronoPay
{
    public class ChronoPayPaymentSettings : ISettings
    {
        public string GatewayUrl { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string SharedSecrect { get; set; }
        public decimal AdditionalFee { get; set; }
    }
}
