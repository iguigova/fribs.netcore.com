using System.Collections.Generic;

namespace com.system.clients
{
    public interface IPaymentClient
    {
        public PaymentToken GetToken(
            string cardNumber = null,
            int? expMonth = null,
            int? expYear = null,
            string cvc = null,
            string addressLine1 = null,
            string addressLine2 = null,
            string city = null,
            string state = null,
            string country = null,
            string zip = null);

        public List<PaymentSubscription> GetSubscriptions(string id);
    }

    public class PaymentSubscription
    {
        public bool Cancelled { get; set; }
        public string Plan { get; set; }
        public string EmailAddress { get; set; }
        public bool HasValidPayment { get; set; }
    }

    public class PaymentToken
    {
        public PaymentToken(string id,
            string number = null,
            long? expMonth = null,
            long? expYear = null,
            string cvc = null,
            string addressLine1 = null,
            string addressLine2 = null,
            string city = null,
            string state = null,
            string country = null,
            string zip = null)
        {
            Id = id;
            Number = number ?? PaymentDefaults.VisaCardNumber;
            ExpMonth = expMonth ?? PaymentDefaults.ExpMonth;
            ExpYear = expYear ?? PaymentDefaults.ExpYear;
            Cvc = cvc ?? PaymentDefaults.Cvc;
            AddressCity = city ?? PaymentDefaults.City;
            AddressLine1 = addressLine1 ?? PaymentDefaults.AddressLine1;
            AddressLine2 = addressLine2 ?? PaymentDefaults.AddressLine2;
            AddressState = state ?? PaymentDefaults.State;
            AddressCountry = country ?? PaymentDefaults.Country;
            AddressZip = zip ?? PaymentDefaults.Zip;
        }

        public string Id { get; private set; }
        public string Number { get; private set; }
        public long ExpMonth { get; private set; }
        public long ExpYear { get; private set; }
        public string Cvc { get; private set; }
        public string AddressCity { get; private set; }
        public string AddressLine1 { get; private set; }
        public string AddressLine2 { get; private set; }
        public string AddressState { get; private set; }
        public string AddressCountry { get; private set; }
        public string AddressZip { get; private set; }
    }

    public static class PaymentDefaults
    {
        public static readonly string VisaCardNumber = "4242424242424242";
        public static readonly string MastercardCardNumber = "5555555555554444";

        public static readonly int ExpMonth = 12;
        public static readonly int ExpYear = 2030;
        public static readonly string Cvc = "123";
        public static readonly string AddressLine1 = "123 Fake St";
        public static readonly string AddressLine2 = "";
        public static readonly string City = "FakeVille";
        public static readonly string State = "BC";
        public static readonly string Country = "CA";
        public static readonly string Zip = "A1A2B2";
    }
}
