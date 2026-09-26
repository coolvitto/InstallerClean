using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.Tests.Helpers;

namespace InstallerClean.Tests.Services.Integration;

public class InstallerQueryServiceTests
{
    [Fact]
    public async Task GetRegisteredPackagesAsync_cancellation_before_start_throws()
    {
        var svc = new InstallerQueryService();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.GetRegisteredPackagesAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_cancellation_token_none_does_not_throw_cancellation()
    {
        var svc = new InstallerQueryService();

        // Default token does not interfere with the call: any thrown
        // exception in this branch is an API failure (UAE without
        // elevation), never OperationCanceledException.
        var ex = await Record.ExceptionAsync(
            () => svc.GetRegisteredPackagesAsync(cancellationToken: CancellationToken.None));

        if (ex is not null)
            Assert.IsNotType<OperationCanceledException>(ex);
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_is_refused_access_exactly_when_the_process_lacks_administrator_rights()
    {
        // The products are enumerated across every account with the Everyone SID.
        // Microsoft documents ERROR_ACCESS_DENIED for a caller without administrator
        // privileges, and the product walk raises it as LocalisedAccessException.
        // Each half is asserted in the process that can reach it: a test run with
        // the rights takes the first, and one without them takes the second.
        var svc = new InstallerQueryService();

        var ex = await Record.ExceptionAsync(() => svc.GetRegisteredPackagesAsync());

        if (AdministratorRights.Held())
            Assert.False(ex is UnauthorizedAccessException,
                $"Refused access with administrator rights: {ex?.GetType().Name}");
        else
            Assert.IsType<LocalisedAccessException>(ex);
    }

    [Fact]
    public async Task GetRegisteredPackagesAsync_null_progress_does_not_throw()
    {
        var svc = new InstallerQueryService();

        // Passing null progress should not cause a NullReferenceException.
        // It may throw UnauthorizedAccessException from the API, which is fine.
        var ex = await Record.ExceptionAsync(
            () => svc.GetRegisteredPackagesAsync(progress: null));

        if (ex is not null)
        {
            Assert.IsNotType<NullReferenceException>(ex);
        }
    }

    // The tests below put the real question to Windows Installer and hold what
    // comes back. Each runs in a process with administrator rights and is
    // reported skipped in one without them, rather than passing without
    // asserting.
    [AdministratorFact]
    public async Task GetRegisteredPackagesAsync_returns_readonly_list_when_elevated()
    {
        var svc = new InstallerQueryService();
        var packages = (await svc.GetRegisteredPackagesAsync()).Packages;

        Assert.IsAssignableFrom<IReadOnlyList<RegisteredPackage>>(packages);
        Assert.NotNull(packages);
    }

    [AdministratorFact]
    public async Task GetRegisteredPackagesAsync_all_paths_non_empty_when_elevated()
    {
        var svc = new InstallerQueryService();
        var packages = (await svc.GetRegisteredPackagesAsync()).Packages;

        Assert.All(packages, p =>
            Assert.False(string.IsNullOrWhiteSpace(p.LocalPackagePath)));
    }

    [AdministratorFact]
    public async Task GetRegisteredPackagesAsync_paths_unique_case_insensitive_when_elevated()
    {
        var svc = new InstallerQueryService();
        var packages = (await svc.GetRegisteredPackagesAsync()).Packages;

        var uniquePaths = new HashSet<string>(
            packages.Select(p => p.LocalPackagePath),
            StringComparer.OrdinalIgnoreCase);

        Assert.Equal(packages.Count, uniquePaths.Count);
    }

    [AdministratorFact]
    public async Task GetRegisteredPackagesAsync_removable_only_when_superseded_when_elevated()
    {
        var svc = new InstallerQueryService();
        var packages = (await svc.GetRegisteredPackagesAsync()).Packages;

        foreach (var pkg in packages.Where(p => p.IsRemovable))
        {
            Assert.True(pkg.PatchState is 2 or 4,
                $"IsRemovable=true but PatchState={pkg.PatchState}");
        }
    }
}
