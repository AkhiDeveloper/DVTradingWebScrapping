using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVTradingWebScrapping.Console
{
    public class StockList
    {
        private readonly IList<ProductStock> _stocks;
        public StockList(string data)
        {
            _stocks = new List<ProductStock>();
            var dataSplit=data.Trim().Split(',');
            foreach(var item in dataSplit)
            {
                try
                {
                    var stockRaw = item.Split(':');
                    _stocks.Add(new ProductStock()
                    {
                        ProductCode = stockRaw[0].GetNumber(),
                        Quantity = Convert.ToInt32(stockRaw[1])
                    });
                }
                catch
                {
                    continue;
                }
                
            }
        }

        public int GetProductQuantityInStock(string productCode)
        {
            var stock = _stocks.FirstOrDefault(x => x.ProductCode == productCode);
            if (stock == null) return 0;
            return stock.Quantity;
        }

    }
}
