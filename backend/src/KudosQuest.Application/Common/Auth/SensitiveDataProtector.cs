using Microsoft.AspNetCore.DataProtection;

namespace KudosQuest.Application.Common.Auth;

public sealed class SensitiveDataProtector(IDataProtectionProvider provider) : ISensitiveDataProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("KudosQuest.StravaTokens.v1");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedPayload) => _protector.Unprotect(protectedPayload);
}
