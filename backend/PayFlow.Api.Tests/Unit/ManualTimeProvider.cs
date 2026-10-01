namespace PayFlow.Api.Tests.Unit;

internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan duration) => now = now.Add(duration);
}
