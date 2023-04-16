using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System.Xml.Schema;
using System.Text.Json.Nodes;

namespace webapi.Controllers;

[ApiController]
[Route("[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly ILogger<PurchasesController> _logger;
    private readonly string _transactionsCsvPath;
    private readonly Dictionary<string, decimal> _itemsDatabase;

    public PurchasesController(ILogger<PurchasesController> logger, IWebHostEnvironment env)
    {
        this._transactionsCsvPath = Path.Combine(env.WebRootPath, "transactions.csv");
        this._logger = logger;

        using (var reader = new StreamReader(Path.Combine(env.WebRootPath, "items.csv")))
        {
            this._itemsDatabase = new Dictionary<string, decimal>();
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                this._itemsDatabase.Add(values[0], Decimal.Parse(values[1]));   
            }
        }
    }

    [HttpGet(Name = "GetPurchases")]
    public IEnumerable<Purchase> Get()
    {
        var purchases = new List<Purchase>();
        using (var reader = new StreamReader(this._transactionsCsvPath))
        {
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                var purchase = new Purchase
                {
                    PurchaseId = Guid.Parse(values[0]),
                    Date = DateTime.Parse(values[1]),
                    Total = Decimal.Parse(values[2]),
                    Cart = values[3],
                    Name = values[4],
                    Ccnum = values[5]
                };

                purchases.Add(purchase);
            }
        }

        return purchases.ToArray();
    }

    [HttpPost(Name = "PostPurchases")]
    public HttpResponseMessage Post([FromBody] JsonObject data)
    {
        HttpResponseMessage response;
        try 
        {
            var purchaseRequest = JsonConvert.DeserializeObject<PurchaseRequest>(data.ToString());
            Decimal total = 0;
            List<string> cart = new List<string>();
            if (purchaseRequest.Soda > 0) 
            {
                total += (this._itemsDatabase["Soda"] * purchaseRequest.Soda);
                cart.Add($"Soda:{purchaseRequest.Soda}");
            }

            if (purchaseRequest.CandyBar > 0)
            {
                total += (this._itemsDatabase["CandyBar"] * purchaseRequest.CandyBar);
                cart.Add($"CandyBar:{purchaseRequest.CandyBar}");
            }

            if (purchaseRequest.Chips > 0) 
            {
                total += (this._itemsDatabase["Chips"] * purchaseRequest.Chips);
                cart.Add($"Chips:{purchaseRequest.Chips}");
            }

            // Don't store  credit card info in database without proper encoding or third party service.
            // But for this excercise Base64 is sufficient.
            string encodedCcnum = Encoder.Base64Encode(purchaseRequest.CreditCardNumber);
            string joinedCart = string.Join(",", cart);
            string row = $"{Guid.NewGuid()}, {DateTime.Now.ToString()}, {total}, { joinedCart}, {purchaseRequest.Name}, {encodedCcnum}";
            using (var writer = new StreamWriter(this._transactionsCsvPath, true))
            {
                writer.WriteLine(row);
            }

            response = new HttpResponseMessage(System.Net.HttpStatusCode.Created);
        }
        catch 
        {
            response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }

        return response;
    }
}
