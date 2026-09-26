using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A fact that runs in a process holding administrator rights on Windows and is
/// reported skipped anywhere else. For a test that puts the scan's own questions to
/// Windows Installer, which answers them across every account only for an
/// administrator (<see cref="AdministratorRights"/>). GitHub documents its Windows
/// runners as running as administrators, so these run in CI.
/// </summary>
public sealed class AdministratorFactAttribute : FactAttribute
{
    public AdministratorFactAttribute()
    {
        if (!OperatingSystem.IsWindows() || !AdministratorRights.Held())
            Skip = "Needs a process with administrator rights on Windows.";
    }
}
