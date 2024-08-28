using LaptopShop.Models;

namespace LaptopShop.ModelViews
{
    public class HomeViewVM
    {
        public List<ProductHomeVM> Products { get; set; }
		public List<Category> Categories { get; set; }
        public List<int> TopProducts { get; set; }
    }
}
