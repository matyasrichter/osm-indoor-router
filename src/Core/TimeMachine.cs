namespace Core;

public interface ITimeMachine
{
    DateTime Now { get; }
}

public class TimeMachine : ITimeMachine
{
    public DateTime Now => DateTime.UtcNow;
}
