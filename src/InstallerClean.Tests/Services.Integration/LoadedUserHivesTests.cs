using System.Security.Principal;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services.Integration;

/// <summary>
/// The accounts the registry fallback lists as having their own hive loaded under
/// <c>HKEY_USERS</c>, which decides whether a per-user unmanaged installation's records
/// can answer for it.
///
/// AN INTEGRATION TEST BECAUSE THE SUBJECT IS WHAT WINDOWS HOLDS UNDER <c>HKEY_USERS</c>.
/// The account running the suite has its own hive loaded while it runs. A signed-in
/// user's <c>_Classes</c> hive is loaded beside it, under the account's name with
/// <c>_Classes</c> after it, and the listing leaves it out. Nothing is written.
/// </summary>
public class LoadedUserHivesTests
{
    [Fact]
    public void The_running_accounts_own_hive_is_listed_as_loaded()
    {
        var running = WindowsIdentity.GetCurrent().User!.Value;

        var loaded = InstallerQueryService.ReadLoadedUserHives();

        Assert.NotNull(loaded);
        Assert.Contains(running, loaded);
    }

    [Fact]
    public void Only_account_names_are_listed()
    {
        var loaded = InstallerQueryService.ReadLoadedUserHives();

        Assert.NotNull(loaded);
        Assert.All(loaded, name => Assert.True(InstallerQueryService.IsAccount(name), name));
        Assert.DoesNotContain(loaded, name => name.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase));
    }
}
