using MyApp.App.Biz;

namespace MyApp.Models
{
    public class HomeViewModel
    {
        public string Json { get; set; }
        public List<Product> ProdList { get; set; }

        public HomeViewModel() { 
            ProdList = new List<Product>();
            Json = "";
        }
    }
}
