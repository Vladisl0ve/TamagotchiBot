using MongoDB.Driver;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.Services.Mongo;

namespace TamagotchiBot.Services
{
    public class PaymentService : MongoServiceBase<StarPayment>, IPaymentService
    {
        public PaymentService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
        }

        public async Task<StarPayment> Create(StarPayment payment)
        {
            await _collection.InsertOneAsync(payment);
            return payment;
        }

        public StarPayment Get(string telegramPaymentChargeId)
        {
            return _collection.Find(p => p.TelegramPaymentChargeId == telegramPaymentChargeId).FirstOrDefault();
        }
    }

    public interface IPaymentService
    {
        Task<StarPayment> Create(StarPayment payment);
        StarPayment Get(string telegramPaymentChargeId);
    }
}
