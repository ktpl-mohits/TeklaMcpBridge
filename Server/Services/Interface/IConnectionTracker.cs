namespace Server.Services.Interface
{
    public interface IConnectionTracker
    {
        void Register(string userId, string connectionId);
        void Unregister(string userId, string connectionId);
        string? GetConnectionId(string userId);
    }
}
