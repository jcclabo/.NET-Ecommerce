using MyApp.App.Biz;
using System.Data;

namespace MyApp.Models
{
    public class AdminViewModel 
    {
        public string Json { get; set; } = "";

        public List<Order> Orders { get; set; } = new List<Order>();

        public List<Product> Products { get; set; } = new List<Product>();
    }

}
