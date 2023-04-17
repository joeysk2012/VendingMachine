using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System.Xml.Schema;
using System.Text.Json.Nodes;
using System.Numerics;

namespace webapi.Controllers;

[ApiController]
[Route("[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly ILogger<PurchasesController> _logger;
    private readonly string _transactionsCsvPath;
    private readonly string _itemsCsvPath;
    private readonly Dictionary<string, (decimal, int)> _itemsDatabase;

    public PurchasesController(ILogger<PurchasesController> logger, IWebHostEnvironment env)
    {
        this._transactionsCsvPath = Path.Combine(env.WebRootPath, "transactions.csv");
        this._itemsCsvPath = Path.Combine(env.WebRootPath, "items.csv");
        this._logger = logger;

        using (var reader = new StreamReader(this._itemsCsvPath))
        {
            this._itemsDatabase = new Dictionary<string, (decimal, int)>();
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                this._itemsDatabase.Add(values[0], (Decimal.Parse(values[1]), int.Parse(values[2])));
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
        string errorMessage = "";
        try
        {
            var purchaseRequest = JsonConvert.DeserializeObject<PurchaseRequest>(data.ToString());
            List<string> cart = new List<string>();
            decimal total = 0;
            int requestedSoda = purchaseRequest.Soda;
            int requestedCandyBar = purchaseRequest.CandyBar;
            int requestedChips = purchaseRequest.Chips;
            errorMessage = this.ValidateRequestAndCreateCart(requestedSoda, requestedCandyBar, requestedChips, out cart, out total);

            if (string.IsNullOrEmpty(errorMessage))
            {
                // Don't store  credit card info in database without proper encoding or third party service.
                // But for this excercise Base64 is sufficient.
                string encodedCcnum = Encoder.Base64Encode(purchaseRequest.CreditCardNumber);
                string joinedCart = string.Join(",", cart);
                string row = $"{Guid.NewGuid()}, {DateTime.Now.ToString()}, {total}, {joinedCart}, {purchaseRequest.Name}, {encodedCcnum}";
                using (var writer = new StreamWriter(this._transactionsCsvPath, true))
                {
                    writer.WriteLine(row);
                }

                this.UpdateItemsDbCsv(this._itemsDatabase);
                response = new HttpResponseMessage(System.Net.HttpStatusCode.Created);
            }
            else
            {
                response = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                response.Content = new StringContent(errorMessage);
            }
        }
        catch
        {
            response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }

        return response;
    }

    [HttpPut(Name = "RefundPurchases")]
    public HttpResponseMessage Put([FromBody] JsonObject data)
    {
        HttpResponseMessage response;
        try
        {
            var refundRequest = JsonConvert.DeserializeObject<RefundRequest>(data.ToString());
            string encodedCcnum = Encoder.Base64Encode(refundRequest.CreditCardNumber);
            if (refundRequest.RefundAmount > 0)
            {
                response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                response.Content = new StringContent("refund must be a negative number");
            }
            else
            {
                string row = $"{Guid.NewGuid()}, {DateTime.Now.ToString()}, {refundRequest.RefundAmount}, , {refundRequest.Name}, {encodedCcnum}";
                using (var writer = new StreamWriter(this._transactionsCsvPath, true))
                {
                    writer.WriteLine(row);
                }

                response = new HttpResponseMessage(System.Net.HttpStatusCode.Created);
            }
        }
        catch
        {
            response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }

        return response;
    }

    private string ValidateRequestAndCreateCart(int requestedSoda, int requestedCandyBar, int requestedChips, out List<string> cart, out decimal total)
    {
        string errorMessage = "";
        cart = new List<string>();
        total = 0;

        if (requestedSoda > 0)
        {
            if (this._itemsDatabase["Soda"].Item2 - requestedSoda < 0)
            {
                errorMessage += "Not enout Soda ";
            }
            else
            {
                total += (this._itemsDatabase["Soda"].Item1 * requestedSoda);
                this._itemsDatabase["Soda"] = (this._itemsDatabase["Soda"].Item1, this._itemsDatabase["Soda"].Item2 - requestedSoda);
                cart.Add($"Soda:{requestedSoda}");
            }
        }

        if (requestedCandyBar > 0)
        {
            if (this._itemsDatabase["CandyBar"].Item2 - requestedSoda < 0)
            {
                errorMessage += "Not enout Candy ";
            }
            else
            {
                total += (this._itemsDatabase["CandyBar"].Item1 * requestedCandyBar);
                this._itemsDatabase["CandyBar"] = (this._itemsDatabase["CandyBar"].Item1, this._itemsDatabase["CandyBar"].Item2 - requestedCandyBar);
                cart.Add($"CandyBar:{requestedCandyBar}");
            }
        }

        if (requestedChips > 0)
        {
            if (this._itemsDatabase["Chips"].Item2 - requestedSoda < 0)
            {
                errorMessage += "Not enout Chips ";
            }
            else
            {
                total += (this._itemsDatabase["Chips"].Item1 * requestedChips);
                this._itemsDatabase["Chips"] = (this._itemsDatabase["Chips"].Item1, this._itemsDatabase["Chips"].Item2 - requestedChips);
                cart.Add($"Chips:{requestedChips}");
            }
        }

        return errorMessage;
    }

    private void UpdateItemsDbCsv(Dictionary<string, (decimal, int)> dictionary)
    {
        using (var writer = new StreamWriter(this._itemsCsvPath))
        {
            // Clear the CSV.
        }

        using (var writer = new StreamWriter(this._itemsCsvPath, true))
        {
            writer.WriteLine("name, cost, quantity");
            foreach (var item in dictionary)
            {
                string row = $"{item.Key}, {item.Value.Item1}, {item.Value.Item2}";
                writer.WriteLine(row);
            }
        }
    }
}
