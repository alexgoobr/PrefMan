using PrefMan.Core.Domain.Dynamo;
using System.Threading.Tasks;

namespace PrefMan.Core.Interfaces
{
    public interface IUserPreferencesRepository
    {
        Task SaveUserPreferences(UserPreferencesDynamo pref);

        Task<UserPreferencesDynamo> GetUserPreferences(string userId);
    }
}
