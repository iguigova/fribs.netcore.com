using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;

namespace com.system.clients
{
    public class StripePaymentClient : IPaymentClient
    {
        protected StripeClient _client;

        protected TokenService _tokenService;
        protected SubscriptionService _subscriptionService;
        protected CustomerService _customerService;

        public StripePaymentClient(string apiKey)
        {
            _tokenService = new TokenService(_client = new StripeClient(apiKey: apiKey));
            _subscriptionService = new SubscriptionService(_client);
            _customerService = new CustomerService(_client);
        }

        public PaymentToken GetToken(
            string cardNumber, 
            int? expMonth = null, 
            int? expYear = null, 
            string cvc = null, 
            string addressLine1 = null, 
            string addressLine2 = null, 
            string city = null, 
            string state = null, 
            string country = null, 
            string zip = null)
        {
            var card = new TokenCardOptions()
            {
                Number = cardNumber,
                ExpMonth = expMonth ?? PaymentDefaults.ExpMonth,
                ExpYear = expYear ?? PaymentDefaults.ExpYear,
                Cvc = cvc ?? PaymentDefaults.Cvc,
                AddressCity = city ?? PaymentDefaults.City,
                AddressLine1 = addressLine1 ?? PaymentDefaults.AddressLine1,
                AddressLine2 = addressLine2 ?? PaymentDefaults.AddressLine2,
                AddressState = state ?? PaymentDefaults.State,
                AddressCountry = country ?? PaymentDefaults.Country,
                AddressZip = zip ?? PaymentDefaults.Zip
            };

            return new PaymentToken(_tokenService.Create(new TokenCreateOptions() { Card = card }).Id, card.Number, card.ExpMonth, card.ExpYear, card.Cvc, card.AddressLine1, card.AddressLine2, card.AddressCity, card.AddressState, card.AddressCountry, card.AddressZip);
        }

        public List<PaymentSubscription> GetSubscriptions(string id)
        {
            var options = new SubscriptionListOptions()
            {
                Created = new AnyOf<DateTime?, DateRangeOptions>(new DateRangeOptions() { GreaterThan = DateTime.UtcNow.AddDays(-1) })
            };

            var subscriptions = new List<PaymentSubscription>();
               
            foreach(var subscription in _subscriptionService.List(options).Where(s => s.Metadata.ContainsValue(id)))
            {
                var customer = _customerService.Get(subscription.CustomerId);

                subscriptions.Add(new PaymentSubscription()
                {
                    Cancelled = subscription.CancelAtPeriodEnd,
                    Plan = null, //subscription.Plan.Nickname,
                    EmailAddress = customer.Email,
                    HasValidPayment = customer.DefaultSourceId != null
                });
            }    
            return subscriptions;
        }
    }
}
