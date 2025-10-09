namespace EasyGames.Services
{
    public interface IPosStateService
    {
        string? GetCustomerId(int shopId);
        string? GetCustomerPhone(int shopId);
        void SetCustomer(int shopId, string userId, string phone);
        void ClearCustomer(int shopId);
    }
}
