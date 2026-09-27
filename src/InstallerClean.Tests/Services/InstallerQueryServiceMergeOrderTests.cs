using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What several API claims on one cached file leave on its row, taken in every order the
/// enumeration could reach them in. The row carries one claim's account of the file, and
/// which one may not turn on the order, and nor may anything else on the row: whether it is
/// removable, the patch state it reads, whether something besides a superseded or obsoleted
/// registration is known to hold it, and so whether the missing-files warning counts it.
///
/// THE CLAIMS ARE BUILT AS THE PRODUCT LOOP BUILDS THEM, one per shape a registration of a
/// patch or a product can arrive in, each from a different product.
/// </summary>
public class InstallerQueryServiceMergeOrderTests
{
    private const string Path = @"C:\Windows\Installer\merge-order.msp";

    /// <summary>
    /// A claim as the product loop builds one: the state parsed from the State read, or zero
    /// where it did not read or did not parse; removable only where
    /// <see cref="InstallerQueryService.IsRemovablePatch"/> says so; unreadable where either
    /// read failed or <see cref="InstallerQueryService.LeavesVerdictUnestablished"/> holds.
    /// </summary>
    private static RegisteredPackage PatchClaim(string name, string? state, string? uninstallable)
    {
        int.TryParse(state, out var patchState);
        var unreadable = state is null || uninstallable is null
            || InstallerQueryService.LeavesVerdictUnestablished(state, uninstallable);
        return new RegisteredPackage(Path, name, "{" + name + "}", patchState,
            IsRemovable: !unreadable && InstallerQueryService.IsRemovablePatch(state!, uninstallable!),
            VerdictUnreadable: unreadable);
    }

    public static TheoryData<string> Kinds() => new(Claims.Keys);

    private static readonly Dictionary<string, RegisteredPackage> Claims = new()
    {
        ["removable superseded"] = PatchClaim("RemovableSuperseded", "2", "0"),
        ["superseded, can be uninstalled"] = PatchClaim("SupersededUninstallable", "2", "1"),
        ["superseded, Uninstallable unread"] = PatchClaim("SupersededUnread", "2", null),
        ["obsoleted"] = PatchClaim("Obsoleted", "4", "0"),
        ["applied"] = PatchClaim("Applied", "1", "0"),
        ["applied, Uninstallable unread"] = PatchClaim("AppliedUnread", "1", null),
        ["State unread"] = PatchClaim("StateUnread", null, "0"),
        ["State empty"] = PatchClaim("StateEmpty", "", "0"),
        ["product"] = new RegisteredPackage(Path, "Product", "{Product}"),
    };

    private static readonly HashSet<string> AppliedClaimants = ["Applied", "AppliedUnread"];

    private static RegisteredPackage Merged(IEnumerable<RegisteredPackage> claims)
    {
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in claims)
            InstallerQueryService.MergeClaim(claimed, claim, InstallerQueryService.ClaimSource.InstallerApi);
        return claimed[Path];
    }

    private static void AssertOneOutcome(IReadOnlyList<RegisteredPackage> claims)
    {
        // The whole row, compared field by field, so the account it carries is held to the
        // order as much as its state and flags.
        var orders = Permutations(claims).ToList();
        var outcomes = orders.Select(Merged).Distinct().ToList();

        Assert.True(outcomes.Count == 1,
            $"[{string.Join(", ", claims.Select(c => c.ProductName))}] merged to "
            + string.Join(" / ", outcomes));

        // Where any claim holds the patch applied, the row names a program that does.
        if (claims.Any(c => AppliedClaimants.Contains(c.ProductName)))
            Assert.All(orders, o => Assert.Contains(Merged(o).ProductName, AppliedClaimants));
    }

    [Fact]
    public void Every_pair_of_claims_leaves_one_outcome_in_either_order()
    {
        foreach (var a in Claims.Values)
            foreach (var b in Claims.Values)
                if (!ReferenceEquals(a, b))
                    AssertOneOutcome([a, b]);
    }

    [Fact]
    public void Every_three_claims_leave_one_outcome_in_every_order()
    {
        var all = Claims.Values.ToList();
        for (var i = 0; i < all.Count; i++)
            for (var j = i + 1; j < all.Count; j++)
                for (var k = j + 1; k < all.Count; k++)
                    AssertOneOutcome([all[i], all[j], all[k]]);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void A_claim_carrying_no_state_marks_a_superseded_or_obsoleted_row(string kind)
    {
        // Whatever reading of the patch the other claim brought, a product's package record
        // naming the file is heard by the warning: the row reads its state and the mark.
        var row = Merged([Claims[kind], Claims["product"]]);

        Assert.Equal(row.IsSupersededOrObsoleted, row.OtherHoldNotRuledOut);
    }

    [Fact]
    public void A_registry_record_of_the_file_leaves_the_row_unmarked()
    {
        // The fallback reads every patch's own package record, so one names every
        // superseded file on every machine. It adds a path nobody else claimed and never
        // speaks to a row that is there.
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);
        InstallerQueryService.MergeClaim(claimed, Claims["removable superseded"],
            InstallerQueryService.ClaimSource.InstallerApi);
        InstallerQueryService.MergeClaim(claimed, new RegisteredPackage(Path, string.Empty, string.Empty),
            InstallerQueryService.ClaimSource.RegistryFallback);

        Assert.False(claimed[Path].OtherHoldNotRuledOut);
        Assert.True(claimed[Path].IsRemovable);
    }

    private static IEnumerable<IReadOnlyList<RegisteredPackage>> Permutations(IReadOnlyList<RegisteredPackage> items)
    {
        if (items.Count <= 1)
        {
            yield return items;
            yield break;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var rest = items.Where((_, index) => index != i).ToList();
            foreach (var tail in Permutations(rest))
                yield return [items[i], .. tail];
        }
    }
}
