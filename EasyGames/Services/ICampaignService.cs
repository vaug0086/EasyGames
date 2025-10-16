using System.Threading.Tasks;

namespace EasyGames.Services
{
    public interface ICampaignService
    {
        Task<int> CreateCampaignAsync(string name, string subject, string bodyHtml, int groupId);
        Task StartNowAsync(int campaignId);
    }
}
