using Braintree;
using MyApp.App.Utils;

namespace MyApp.App.PaymentGateway
{
    /// <summary>
    /// Used to set up a payment gateway via the Braintree Server SDK
    /// </summary>
    public class BraintreeConfiguration : IBraintreeConfiguration
    {
        public string Environment { get; set; }
        public string MerchantId { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        private IBraintreeGateway BraintreeGateway { get; set; }

        /// <summary>
        /// Sets up a payment gateway using enviroment variables
        /// </summary>
        /// <returns>IBraintreeGateway</returns>
        public IBraintreeGateway CreateGateway()
        {
            Environment = System.Environment.GetEnvironmentVariable("BraintreeEnvironment");
            MerchantId = System.Environment.GetEnvironmentVariable("BraintreeMerchantId");
            PublicKey = System.Environment.GetEnvironmentVariable("BraintreePublicKey");
            PrivateKey = System.Environment.GetEnvironmentVariable("BraintreePrivateKey");

            if (MerchantId == null || PublicKey == null || PrivateKey == null)
            {
                Environment = AppSettings.GetString("BraintreeEnvironment");
                MerchantId = AppSettings.GetString("BraintreeMerchantId");
                PublicKey = AppSettings.GetString("BraintreePublicKey");
                PrivateKey = AppSettings.GetString("BraintreePrivateKey");
            }

            return new BraintreeGateway(Environment, MerchantId, PublicKey, PrivateKey);
        }

        /// <summary>
        /// Gets existing gateway or sets up a new gateway using environment variables
        /// </summary>
        /// <returns>IBraintreeGateway</returns>
        public IBraintreeGateway GetGateway()
        {
            if (BraintreeGateway == null)
            {
                BraintreeGateway = CreateGateway();
            }

            return BraintreeGateway;
        }
    }
}
