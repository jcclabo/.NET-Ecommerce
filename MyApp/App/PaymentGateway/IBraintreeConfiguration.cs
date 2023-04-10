using Braintree;

namespace MyApp.App.PaymentGateway
{
    /// <summary>
    /// Interface for secure payment gateway configuration with the Braintree server SDK
    /// </summary>
    public interface IBraintreeConfiguration
    {
        IBraintreeGateway CreateGateway();
        IBraintreeGateway GetGateway();
    }
}
