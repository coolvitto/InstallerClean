using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class SweepTravelTests
{
    /// <summary>
    /// The scanning overlay's bar, and the splash's at 100 and 200 per cent text,
    /// whose width follows the window's.
    /// </summary>
    [Theory]
    [InlineData(240)]
    [InlineData(384)]
    [InlineData(864)]
    public void The_stripe_crosses_the_whole_track_at_one_speed(double trackWidth)
    {
        var (from, to, duration) = SweepTravel.For(80, trackWidth);

        // Wholly off the left end at the start and wholly off the right end at the
        // finish, however wide the track.
        Assert.Equal(-80, from);
        Assert.Equal(trackWidth, to);
        Assert.Equal(SweepTravel.PixelsPerSecond, (to - from) / duration.TotalSeconds, 2);
    }
}
