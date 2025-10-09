using Microsoft.AspNetCore.Http;

namespace EasyGames.Services
{
    public sealed class PosStateService : IPosStateService
    {
        private readonly ISession _session;
        public PosStateService(IHttpContextAccessor accessor)
        {
            _session = accessor.HttpContext!.Session;
        }

        private static string K(int shopId, string suffix) => $"pos:{shopId}:{suffix}";

        public string? GetCustomerId(int shopId) => _session.GetString(K(shopId, "custId"));
        public string? GetCustomerPhone(int shopId) => _session.GetString(K(shopId, "custPhone"));

        public void SetCustomer(int shopId, string userId, string phone)
        {
            _session.SetString(K(shopId, "custId"), userId);
            _session.SetString(K(shopId, "custPhone"), phone);
        }

        public void ClearCustomer(int shopId)
        {
            _session.Remove(K(shopId, "custId"));
            _session.Remove(K(shopId, "custPhone"));
        }
    }
}
