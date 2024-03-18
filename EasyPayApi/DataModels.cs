namespace EasyPayApi
{
    /// <summary>
    /// 
    /// </summary>
    public class SalesOrder
    {
        public int? SalesOrderId { get; set; }
        public string? Datetime { get; set; }
        public string Username { get; set; }
        public string? Currency { get; set; }
        public List<LineItem> LineItems { get; set; }
        public ShippingMethod ShippingId { get; set; }
        public string StripeApiKey { get; set; }
        public string? SuccessUrl { get; set; }
        public string? CancelUrl { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LineItem
    {
        public decimal Price { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ShippingMethod
    {
        public int ShippingId { get; set; }
        public decimal Price { get; set; }
        public string? Title { get; set; }
        public int DaysMini { get; set; }
        public int DaysMaxi { get; set; }
    }

    public class Account
    {
        public string email { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string stripe_key { get; set; }
    }

    public class LoginForm
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class SalesOrderForm 
    {
        public int? id { get; set; }
        public string username { get; set; }
        public string name { get; set; }
        public decimal price { get; set; }
        public string currency { get; set; }
        public string image { get; set; }
        public string? return_url { get; set; }
    }

    public class UserCatalogRequest
    {
        public string username { get; set; }
    }

    public class UpdateUsernameRequest
    {
        public string current_username { get; set; }
        public string new_username { get; set; }
        public string password { get; set; }
    }

    public class UpdatePasswordRequest
    {
        public string username { get; set; }
        public string current_password { get; set; }
        public string new_password { get; set; }
    }

    public class UpdateEmailRequest
    {
        public string username { get; set; }
        public string new_email { get; set; }
        public string password { get; set; } 
    }

    public class UpdateStripeKeyRequest
    {
        public string username { get; set; }
        public string password { get; set; }
        public string stripe_key { get; set; }
    }
}