namespace InstallerClean.Services;

/// <summary>
/// The account this process runs as: the user its token carries, which under elevation
/// with another administrator's credentials is that administrator rather than whoever
/// is signed in, and for the command line run by a scheduled task can be LocalSystem.
/// </summary>
public interface IRunningAccount
{
    /// <summary>
    /// The account's SID in its string form, <c>S-1-5-...</c>, or null where it was not
    /// read. Null matches no installation's account.
    /// </summary>
    string? Sid { get; }
}
