namespace Mikodev.Links.Sharing
{
    public enum ShareStatus : int
    {
        None = default,
        Pending = 1,
        Running,
        Pausing,

        Success = Completed | 1,
        Aborted,
        Refused,

        Completed = 0x8000,
    }
}
