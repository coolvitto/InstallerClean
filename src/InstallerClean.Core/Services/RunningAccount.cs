using System.Security;
using System.Security.Principal;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IRunningAccount"/>: the user SID of this process's own token,
/// read once, on first use, and held for the life of the process.
///
/// READ ON FIRST USE RATHER THAN AT CONSTRUCTION, so building the service graph asks
/// nothing of the token. A read that throws <see cref="SecurityException"/>, which is the
/// refusal Microsoft documents for <see cref="WindowsIdentity.GetCurrent()"/>, answers
/// null, which matches no account.
/// </summary>
public sealed class RunningAccount : IRunningAccount
{
    private readonly Lazy<string?> _sid = new(Read);

    /// <inheritdoc />
    public string? Sid => _sid.Value;

    private static string? Read()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value;
        }
        catch (SecurityException)
        {
            return null;
        }
    }
}
