interface IOrderPricing
{
    OrderPrice? Calculate(IReadOnlyList<CartItem?> items);
}

record OrderPrice(decimal Subtotal, decimal CupFee);

sealed class OrderPricing : IOrderPricing
{
    private const decimal CustomCupPrice = 45m;

    public OrderPrice? Calculate(IReadOnlyList<CartItem?> items)
    {
        decimal subtotal = 0;
        decimal cupFee = 0;

        foreach (var item in items)
        {
            if (item is null)
                return null;

            var product = Catalog.Products.FirstOrDefault(product => product.Id == item.ProductId);
            if (product is null || item.Quantity is < 1 or > 20 || item.CupText?.Length > 24)
                return null;

            if (item.CustomCup && (product.Category != "coffee" || item.CupColor is not ("pink" or "green" or "purple")))
                return null;

            subtotal += product.Price * item.Quantity;
            if (item.CustomCup)
                cupFee += CustomCupPrice * item.Quantity;
        }

        return new OrderPrice(subtotal, cupFee);
    }
}
