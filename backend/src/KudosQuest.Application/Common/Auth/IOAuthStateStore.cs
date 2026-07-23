namespace KudosQuest.Application.Common.Auth;

public interface IOAuthStateStore
{
    string Create();
    bool TryConsume(string state);
}
