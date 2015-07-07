using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.WarmStorage
{
    public interface IBuildingLookupService
    {
        Task<string> GetBuildingIdAsync(string deviceId);
    }
}