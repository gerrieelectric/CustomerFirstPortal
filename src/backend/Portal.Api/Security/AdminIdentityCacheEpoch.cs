using System.Globalization;
using System.Threading;

namespace Portal.Api.Security;

public sealed class AdminIdentityCacheEpoch : IAdminIdentityCacheEpoch
{
    private long _epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public string GetCurrentEpoch()
    {
        var epoch = Interlocked.Read(ref _epoch);
        return epoch.ToString(CultureInfo.InvariantCulture);
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _epoch);
    }
}
