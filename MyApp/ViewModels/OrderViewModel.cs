
using MyApp.App.Utils;

namespace MyApp.Models
{
    public class OrderViewModel
    {
        public string JsonOrder { get; set; }

        public string ClientToken { get; set; }

        public decimal ExpressShippingCost { get; set; }

        public OrderViewModel() {
            JsonOrder = "";
            ClientToken = "";
            ExpressShippingCost = decimal.Parse(AppSettings.GetString("ExpressShippingCost"));
        }

    }
}
