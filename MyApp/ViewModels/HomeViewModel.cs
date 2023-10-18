using MyApp.App.Biz;

namespace MyApp.Models
{
    public class HomeViewModel
    {
        public string Json { get; set; } = "";
        public List<Product> ProdList { get; set; } = new List<Product>();

    }
}
