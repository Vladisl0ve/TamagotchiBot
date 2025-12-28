using System;
using MongoDB.Bson.Serialization.Attributes;

namespace TamagotchiBot.Models.Mongo
{
    public class StarPayment : MongoModelBase
    {
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("TelegramPaymentChargeId")]
        public string TelegramPaymentChargeId { get; set; }

        [BsonElement("ProviderPaymentChargeId")]
        public string ProviderPaymentChargeId { get; set; }

        [BsonElement("Amount")]
        public int Amount { get; set; }

        [BsonElement("Currency")]
        public string Currency { get; set; }

        [BsonElement("PaymentDate")]
        public DateTime PaymentDate { get; set; }
    }
}
