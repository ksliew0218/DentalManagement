namespace DentalManagement.Models
{
    public class StripeSettings
    {
        public string SecretKey { get; set; }
        public string PublishableKey { get; set; }
        public string WebhookSecret { get; set; }
        public string Currency { get; set; } = "myr"; // Default to Malaysian Ringgit
    }
}