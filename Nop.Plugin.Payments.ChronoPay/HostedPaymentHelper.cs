using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Nop.Plugin.Payments.ChronoPay
{
    public class HostedPaymentHelper
    {
        #region Methods

        public static string CalcRequestSign(NameValueCollection reqParams, string sharedSecrect)
        {
            return CalcMd5Hash($"{reqParams["product_id"]}-{reqParams["product_price"]}-{sharedSecrect}");
        }

        public static bool ValidateResponseSign(IFormCollection rspParams, string sharedSecrect)
        {
            var rspSign = rspParams["sign"];
            if (string.IsNullOrEmpty(rspSign))
            {
                return false;
            }
            return rspSign.Equals(CalcMd5Hash($"{sharedSecrect}{rspParams["customer_id"]}{rspParams["transaction_id"]}{rspParams["transaction_type"]}{rspParams["total"]}"));
        }

        #endregion

        #region Utilities

        private static string CalcMd5Hash(string s)
        {
            using (var cs = MD5.Create())
            {
                var sb = new StringBuilder { Length = 0 };

                foreach (var b in cs.ComputeHash(Encoding.UTF8.GetBytes(s)))
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        #endregion
    }
}
