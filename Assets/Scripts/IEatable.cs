public interface IEatable
{
    /// <summary>尝试被吃 1 口。成功返回 true，没得吃返回 false。</summary>
    bool ConsumeOneUnit();

    /// <summary>剩余可被吃的单位口数。</summary>
    int UnitsRemaining { get; }

    bool IsDepleted { get; }
}
