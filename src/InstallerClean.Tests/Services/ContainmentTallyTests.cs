using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// The count the scan keeps of the files the containment check kept back, by its
/// verdict. Both halves of the scan count through it: the folder walk's files into the
/// withholding split, and the superseded rows into counts of their own. These pin the
/// mapping apart from any scan, the unproven verdict included.
/// </summary>
public class ContainmentTallyTests
{
    [Fact]
    public void A_refusal_and_an_unproven_verdict_each_count_into_their_own_member()
    {
        var tally = new FileSystemScanService.ContainmentTally();

        Assert.True(tally.Record(CandidateGuard.RemovalSafety.Refused));
        Assert.True(tally.Record(CandidateGuard.RemovalSafety.Unproven));
        Assert.True(tally.Record(CandidateGuard.RemovalSafety.Unproven));

        Assert.Equal(1, tally.RefusedCount);
        Assert.Equal(2, tally.UnestablishedCount);
    }

    [Fact]
    public void Safe_and_a_verdict_the_tally_does_not_name_count_nowhere_and_say_so()
    {
        // Safe never reaches the tally, the callers asking first; passing it anyway pins
        // that it would be counted under neither. The cast value is the state a verdict
        // added to the enum would arrive in, and counting it under Refused or Unproven
        // would put a finding on the file that nobody established. The false return is
        // what keeps a caller from sizing a file it did not count.
        var tally = new FileSystemScanService.ContainmentTally();

        Assert.False(tally.Record(CandidateGuard.RemovalSafety.Safe));
        Assert.False(tally.Record((CandidateGuard.RemovalSafety)99));

        Assert.Equal(0, tally.RefusedCount);
        Assert.Equal(0, tally.UnestablishedCount);
    }

    [Fact]
    public void Every_verdict_but_Safe_counts_into_a_member_of_its_own()
    {
        // Driven from the enum rather than from the verdicts the switch names, so a
        // member added to it arrives here asking for a count instead of being kept back
        // and counted under nothing.
        var keeps = Enum.GetValues<CandidateGuard.RemovalSafety>()
            .Where(v => v != CandidateGuard.RemovalSafety.Safe)
            .ToArray();

        // A list that came back empty would leave the loop below checking nothing, which
        // reads exactly like a clean result.
        Assert.True(keeps.Length >= 2, "the verdict list came back short");

        var moved = new HashSet<string>();
        foreach (var verdict in keeps)
        {
            var tally = new FileSystemScanService.ContainmentTally();

            Assert.True(tally.Record(verdict), $"{verdict} was not counted");

            var members = new List<string>();
            if (tally.RefusedCount == 1) members.Add(nameof(tally.RefusedCount));
            if (tally.UnestablishedCount == 1) members.Add(nameof(tally.UnestablishedCount));

            var member = Assert.Single(members);
            Assert.True(moved.Add(member), $"{verdict} counts into {member}, which another verdict already does");
        }
    }
}
