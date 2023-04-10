
using Braintree;

namespace MyApp.Models
{
    public class OrderViewModel
    {
        public string JsonOrder { get; set; } = "";

        public string ClientToken { get; set; } = "";
        //public Transaction Transaction { get; set; }   // the Transaction is accesssible via the vue data componenet's "order" property
    }
}
