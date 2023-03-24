public class Order
{
    //public int group_id { get; set; }
    //public int outlet_id { get; set; }
    //public int product_id { get; set; }
    //public int quantity { get; set; }
    //public int distributor_id { get; set; }
    //public string discount { get; set; }
    public int id { get; set; }
    public int quantity { get; set; }
    public IList<Stock> stock { get; set; }
    public Product product { get; set; }

    public decimal Amount { get => quantity * product.rlp; }
}