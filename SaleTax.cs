using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
					
public class Program
{
	public static void Main()
	{
         var input1 = new[]
        {
            "1 book at 12.49",
            "1 music CD at 14.99",
            "1 chocolate bar at 0.85"
        };

        var input2 = new[]
        {
            "1 imported box of chocolates at 10.00",
            "1 imported bottle of perfume at 47.50"
        };

        var input3 = new[]
        {
            "1 imported bottle of perfume at 27.99",
            "1 bottle of perfume at 18.99",
            "1 packet of headache pills at 9.75",
            "1 box of imported chocolates at 11.25"
        };


        ProcessInput(input1);
        Console.WriteLine("--------------------------------------------------");

        ProcessInput(input2);
        Console.WriteLine("--------------------------------------------------");

        ProcessInput(input3);
        Console.WriteLine("--------------------------------------------------");
    }

    private static void ProcessInput(string[] input)
    {
        foreach (var item in input)
        {
            Console.WriteLine(item);
        }
        Console.WriteLine();

        var shoppringCart = ItemParser.Parse(input);

        var taxCalculator = new TaxCalculator();
        taxCalculator.Calculate(shoppringCart);

		//Discounting can be applied here!
		
        ShopingCartPrinter.Print(shoppringCart);
    }
}

enum ProductType
{
    Food,
    Medical,
    Book,
    Other
}

class Product
{

    private static readonly IDictionary<ProductType, string[]> itemType_Identifiers = new Dictionary<ProductType, string[]>
    {
        {ProductType.Food, new[]{ "chocolate", "chocolates" }},
        {ProductType.Medical, new[]{ "pills" }},
        {ProductType.Book, new[]{ "book" }}
    };

    public decimal ShelfPrice { get; set; }

    public string Name { get; set; }

    public bool IsImported { get { return Name.Contains("imported "); } }

    public bool IsTypeOf(ProductType productType)
    {
        return itemType_Identifiers.ContainsKey(productType) &&
            itemType_Identifiers[productType].Any(x => Name.Contains(x));
    }

    public override string ToString()
    {
        return string.Format("{0} at {1}", Name, ShelfPrice);
    }

}

#region Tax
abstract class SalesTax
{
    abstract public bool IsApplicable(Product item);
    abstract public decimal Rate { get; }

    public decimal Calculate(Product item)
    {
        if (IsApplicable(item))
        {
            //sales tax are that for a tax rate of n%, a shelf price of p contains (np/100)
            var tax = (item.ShelfPrice * Rate) / 100;

            //The rounding rules: rounded up to the nearest 0.05
            tax = Math.Ceiling(tax / 0.05m) * 0.05m;

            return tax;
        }

        return 0;
    }
}

class BasicSalesTax : SalesTax
{
    private ProductType[] _taxExcemptions = new[] { ProductType.Food, ProductType.Medical, ProductType.Book };

    public override bool IsApplicable(Product item)
    {
        return !(_taxExcemptions.Any(x => item.IsTypeOf(x)));
    }

    public override decimal Rate { get { return 10.00M; } }
}

class ImportedDutySalesTax : SalesTax
{
    public override bool IsApplicable(Product item)
    {
        return item.IsImported;
    }

    public override decimal Rate { get { return 5.00M; } }
}

class TaxCalculator
{
    private SalesTax[] _Taxes = new SalesTax[] { new BasicSalesTax(), new ImportedDutySalesTax() };

    public void Calculate(ShoppringCart shoppringCart)
    {
        foreach (var cartItem in shoppringCart.CartItems)
        {
            cartItem.Tax = _Taxes.Sum(x => x.Calculate(cartItem.Product));
        }
    }
}

#endregion Tax

class ShoppringCart
{
    public IList<ShoppringCartItem> CartItems { get; set; }

    public decimal TotalTax { get { return CartItems.Sum(x => x.Tax); } }

    public decimal TotalCost { get { return CartItems.Sum(x => x.Cost); } }
}

class ShoppringCartItem
{
    public Product Product { get; set; }

    public int Quantity { get; set; }

    public decimal Tax { get; set; }

    public decimal Cost { get { return Quantity * (Tax + Product.ShelfPrice); } }

    public override string ToString()
    {
        return string.Format("{0} {1} : {2:N2}", Quantity, Product.Name, Cost);
    }
}

class ItemParser
{
    private static readonly string ITEM_ENTRY_REGEX = "(\\d+) ([\\w\\s]* )at (\\d+.\\d{2})";

    private static readonly string[] food_identifier = { "chocolate", "chocolates" };
    private static readonly string[] medical_identifier = { "pills" };
    private static readonly string[] book_identifier = { "book" };

    public static ShoppringCart Parse(string[] listOfItemfullDesc)
    {
        return new ShoppringCart
        {
            CartItems = listOfItemfullDesc.Select(Parse).ToList(),
        };
    }

    public static ShoppringCartItem Parse(string itemfullDesc)
    {
        var regex = new Regex(ITEM_ENTRY_REGEX);
        var match = regex.Match(itemfullDesc);
        if (match.Success)
        {
            var quantity = int.Parse(match.Groups[1].Value);
            var price = decimal.Parse(match.Groups[3].Value);

            var itemName = match.Groups[2].Value.Trim();

            var shopp = new ShoppringCartItem
            {
                Quantity = quantity,
                Product = new Product { Name = itemName, ShelfPrice = price }
            };

            return shopp;
        }

        return null;
    }
}

class ShopingCartPrinter
{
    public static void Print(ShoppringCart shoppringCart)
    {
        //printe items => 1 chocolate bar: 0.85
        foreach (var cartItem in shoppringCart.CartItems)
        {
            Console.WriteLine(cartItem.ToString());
        }

        //print Sales => Taxes: 1.50
        Console.WriteLine("Taxes: {0:N2}", shoppringCart.TotalTax);

        //print => Total: 29.83
        Console.WriteLine("Total: {0:N2}", shoppringCart.TotalCost);
    }
}