namespace PayFlow.Features.Authentication.Interfaces
{
    public interface ICurrentUser
    {
        Guid UserId { get; }

        bool IsAuthenticated { get; }
    }
}
