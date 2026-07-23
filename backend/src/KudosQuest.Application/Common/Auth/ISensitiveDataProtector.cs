namespace KudosQuest.Application.Common.Auth;

public interface ISensitiveDataProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedPayload);
}
