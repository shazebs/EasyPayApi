using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using System.Net;

namespace EasyPayApi
{
    public class StripeService : IDisposable
    {
        private bool disposed = false;        

        public StripeService(string stripekey) 
        {
            StripeConfiguration.ApiKey = stripekey;
        }

        // Use this method to close or release unmanaged resources
        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free any other managed objects here.
                }
                disposed = true;
            }
        }

        /// <summary>
        /// (UNUSED) For handling multiple catalog items in one cart checkout.
        /// </summary>
        /// <param name="salesOrder"></param>
        /// <returns></returns>
        public string GeneratePaymentPortal(SalesOrder salesOrder)
        {
            var lineItems = new List<SessionLineItemOptions>();

            foreach (var item in salesOrder.LineItems)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = item.Price * 100,
                        Currency = salesOrder.Currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Name,
                            Images = new List<string>() { item.Image }
                        },
                    },
                    Quantity = item.Quantity
                });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                LineItems = lineItems,
                ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                {
                    AllowedCountries = new List<string> { "US", "AE" },
                },
                ShippingOptions = new List<SessionShippingOptionOptions>
                {
                    new SessionShippingOptionOptions
                    {
                        ShippingRateData = new SessionShippingOptionShippingRateDataOptions
                        {
                            Type = "fixed_amount",
                            FixedAmount = new SessionShippingOptionShippingRateDataFixedAmountOptions
                            {
                                Amount = (long) (salesOrder.ShippingId.Price * 100),
                                Currency = salesOrder.Currency,
                            },
                            DisplayName = salesOrder.ShippingId.Title,
                            DeliveryEstimate = new SessionShippingOptionShippingRateDataDeliveryEstimateOptions
                            {
                                Minimum = new SessionShippingOptionShippingRateDataDeliveryEstimateMinimumOptions
                                {
                                    Unit = "business_day",
                                    Value = salesOrder.ShippingId.DaysMini,
                                },
                                Maximum = new SessionShippingOptionShippingRateDataDeliveryEstimateMaximumOptions
                                {
                                    Unit = "business_day",
                                    Value = salesOrder.ShippingId.DaysMaxi,
                                },
                            },
                        },
                    },
                },
                Mode = "payment",
                SuccessUrl = salesOrder.SuccessUrl,
                CancelUrl = salesOrder.CancelUrl
            };
            var service = new SessionService();
            Session session = service.Create(options);
            return session.Url;
        }

        /// <summary>
        /// Generate single item checkout pages.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string GeneratePaymentPortal_v2(SalesOrderForm item)
        {
            // prod
            var return_url = "https://easypaytest-80d7a65f94b6.herokuapp.com";

            // dev
            //var return_url = "http://localhost:8080";

            var lineItems = new List<SessionLineItemOptions>()
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = item.price * 100,
                        Currency = item.currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.name,
                            Images = new List<string>() { item.image },
                        },
                    },
                    Quantity = 1
                }
            };
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                LineItems = lineItems,
                ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                {
                    AllowedCountries = new List<string> { "US" },
                },
                ShippingOptions = null,
                Mode = "payment",
                SuccessUrl = $"{return_url}/receipt",
                CancelUrl = item.return_url,
                Metadata = new()
                {
                    { "name", item.name },
                    { "price", $"{item.price}" },
                    { "currency", item.currency },
                    { "image", item.image },
                    { "username", item.username }
                }
            };

            var service = new SessionService();
            Session session = service.Create(options);
            return session.Url;
        }

        // De-Constructor.
        ~StripeService()
        {
            Dispose(false);
        }

    }
}
