// See https://aka.ms/new-console-template for more information
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using System.Text.Json;
using System.Collections.Specialized;
using System.Text;
using DVTradingWebScrapping.Console;

try
{
    var security_key = "D1x62$aW3#gf";
    Console.Write("Enter the Security key:\t");
    var entered_key = Console.ReadLine();
    if(entered_key != security_key)
    {
        throw new Exception("\nSecurity Key entered is wrong!");
    }
    ScrapingBrowser browser = new ScrapingBrowser();
    browser.KeepAlive = true;
    var baselink = new Uri(@"https://haldiram.dvtrading.com.np");
    //load statup page
    var pageResponse = await browser.NavigateToPageAsync(baselink.Combine("login"));

    //submit login form
    Console.WriteLine("!----------- SignIn -----------!");
    Console.Write("Enter the phone number:\t");
    var phone = Console.ReadLine();
    Console.Write("Enter the password:\t");
    var password = Console.ReadLine();
    Console.Write("How many days before today want to conduct process :\t");
    var days = Int32.Parse(Console.ReadLine() ?? "0");
    var loginform = pageResponse.FindForm("");
    //var phone = "9843888939";
    //var password = "123456";

    foreach (var formfield in loginform.FormFields)
    {
        if (formfield.Name == "phone")
        {
            formfield.Value = phone;
            continue;
        }
        if (formfield.Name == "password")
        {
            formfield.Value = password;
            continue;
        }
    }
    loginform.Submit();

    //Navigating to order page
    Console.WriteLine();
    var now = DateTime.Now.ToString("yyyy-MM-dd");
    var from = (DateTime.Now - new TimeSpan(days, 0, 0, 0)).ToString("yyyy-MM-dd");
    var adminUri = baselink.Combine("admin");
    var totalBillCount = 0;
    var totalOrderCount = 0;

    //Detecting Number of Pages
    int pageCount = 0;
    int maxPages = 50;
    Console.Clear();
    Console.WriteLine("!--------- Page are Loading -----------!");
    IList<WebPage> webPages = new List<WebPage>();
    do
    {
        try
        {
            pageCount++;
            var orderPage = await browser.NavigateToPageAsync(adminUri.Combine($"orders/filter?from={from}&now={now}&page={pageCount}"));
            var outletlist = orderPage.Html.CssSelect("table").FirstOrDefault(x => x.Id == "outlets-list-table").CssSelect("tr");
            if (outletlist.Count() < 2)
                break;
            webPages.Add(orderPage);
            Console.Write("|");
        }
        catch
        {
            break;
        }
        
    } while (pageCount < maxPages);
    pageCount--;

    //Navigating to pages
    Console.Clear() ;
    int pageNumber = 0;
    foreach (var page in webPages )
    {
        pageNumber++;
        Console.WriteLine($"!-------- Page {pageNumber} out of {pageCount} --------!");
        //var orderPage = await browser.NavigateToPageAsync(adminUri.Combine($"orders/filter?from={from}&now={now}"));
        var _token = page.FindFormById("logout-form").FormFields.FirstOrDefault(x => x.Name == "_token").Value;
        var outletlist = page.Html.CssSelect("table").FirstOrDefault(x => x.Id == "outlets-list-table");
        if (outletlist == null)
        {
            Console.WriteLine("No Order are found.");
            return;
        }
        var rows = outletlist.CssSelect("tr");
        if (rows.Count() < 2)
        {
            Console.WriteLine("No Order are found.");
            return;
        }
        int count = 0;
        foreach (var row in rows)
        {
            try
            {
                var cells = row.CssSelect("td");
                if (cells.Count() < 2) continue;
                count++;
                var outletName = cells.ElementAt(5).InnerText.Trim();
                var date = cells.ElementAt(2).InnerText.Trim();
                Console.WriteLine($"{count}.\tProcess Started for {outletName} at {date}:");
                var GID = cells.ElementAt(1).InnerHtml.Trim();
                var orderJsonString = await browser.DownloadStringAsync(adminUri.Combine("orders").Combine(GID).Combine("api_show_order").Combine(date));
                IList<Order> orders = JsonSerializer.Deserialize<IList<Order>>(orderJsonString);
                
                StringBuilder orderIds = new StringBuilder();
                int addedOrder = 0;
                for (int i = 0; i < orders.Count(); i++)
                {
                    StockList stocks = new StockList(orders[i].stock[0].stock);
                    if (orders[i].quantity > stocks.GetProductQuantityInStock(orders[i].product.id.ToString()))
                    {
                        orders.Remove(orders[i]);
                        i--;
                        continue;
                    }
                    addedOrder++;
                    if (addedOrder < 2)
                    {
                        orderIds.Append(orders[i].id.ToString());
                        continue;
                    }
                    orderIds.Append(',' + orders[i].id.ToString());
                }
                if (orders.Count() < 1) throw new Exception("This order have no stock available are for any product.");
                //Calling Invoice page
                NameValueCollection invoiceRequest = new NameValueCollection()
                {
                    {"_token",_token },
                    {"arr", orderIds.ToString() }
                };

                WebPage invoicePage = browser.NavigateToPage(
                    adminUri.Combine("invoice").Combine("show"),
                    HttpVerb.Post, invoiceRequest);

                var invoiceForm = invoicePage.FindFormById("invoiceSubmitForm");

                invoiceForm.FormFields.FirstOrDefault(x => x.Name == "bill_discount_amount").Value = "0";
                invoiceForm.FormFields.FirstOrDefault(x => x.Name == "sub_total").Value = decimal.Round(orders.Sum(x => x.Amount),2,MidpointRounding.ToEven).ToString();
                invoiceForm.FormFields.FirstOrDefault(x => x.Name == "total_amount").Value = decimal.Round(orders.Sum(x => x.Amount) * 1.13m,2,MidpointRounding.ToEven).ToString();
                invoiceForm.Submit();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tSuccess");
                Console.ForegroundColor = ConsoleColor.White;
                totalBillCount++;
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\tFailed.");
                Console.WriteLine($"\t{ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
                continue;
            }
        }
        totalOrderCount += count;
    }
    Console.WriteLine();
    Console.WriteLine($"Processed Orders:\t{totalOrderCount}");
    Console.WriteLine($"Created Bills:\t{totalBillCount}");
}
catch (Exception ex)
{
    Console.Clear();
    Console.ForegroundColor= ConsoleColor.Red;
    Console.WriteLine("Error : ");
    Console.WriteLine(ex.Message);
    Console.ForegroundColor = ConsoleColor.White;
}
finally
{
    Console.Beep();
    Console.WriteLine();
    Console.WriteLine("Press any key to exit");
    Console.ReadKey();
    Console.Clear();
}

//using (var client = new HttpClient())
//{
//    try
//    {

//        //load startup page
//        var pageResponse = await client.GetAsync(@"http://103.94.159.128:5062/dv_trading/login");
//        var pageHtml = pageResponse.Content;
//        Console.WriteLine("1. Login stat-up page is sucessfully loaded");

//        //extract token from page
//        Console.WriteLine("2. Token sucessfully extracted.");

//        //login
//        Console.Write("Enter the phone number: ");
//        var phone = Console.ReadLine();
//        Console.Write("Enter the password: ");
//        var password = Console.ReadLine();
//        var token = Console.ReadLine();
//        var values = new Dictionary<string, string>
//        {
//            { "_token", token },
//            { "phone", phone },
//            {"password",password }
//        };

//        var content = new FormUrlEncodedContent(values);
//        var loginresponse = await client.PostAsync(@"http://103.94.159.128:5062/dv_trading/login", content);
//        if(loginresponse.IsSuccessStatusCode)
//        {
//            Console.WriteLine($"Successfully logged in");
//        }
//        var orderresponse = await client.GetAsync(@"http://103.94.159.128:5062/dv_trading/admin/orders");

//        var responseString = await orderresponse.Content.ReadAsStringAsync();
//        var orderPath = Path.Combine(Environment.CurrentDirectory, "order.html");
//        using (Stream stream = new FileStream(orderPath, FileMode.Create))
//        {
//            using (StreamWriter reader = new StreamWriter(stream))
//            {
//                reader.WriteLine(responseString);
//            }
//        }
//        Console.WriteLine($"Success saved to {orderPath}");
//    }
//    catch(Exception ex)
//    {
//        Console.WriteLine(ex.Message);
//    }
    
    
//}