namespace Portal.Api.Security;

public interface IAdminIdentityCacheEpoch
{
    string GetCurrentEpoch();
    void Invalidate();
}
