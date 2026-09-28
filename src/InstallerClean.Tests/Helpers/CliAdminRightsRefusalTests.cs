using System.Globalization;
using InstallerClean.Cli;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The Application-channel line a <c>/s</c>, <c>/d</c> or <c>/m</c> run writes when
/// it is refused for want of administrator rights, which is the line an RMM matches
/// on, read back through the method that builds it.
/// </summary>
public class CliAdminRightsRefusalTests
{
    [Theory]
    [InlineData("/s")]
    [InlineData("/d")]
    [InlineData("/m")]
    public void The_event_log_line_is_these_words_with_the_flag_first(string arg)
    {
        // The whole line, because Application-log tooling matches on its words, and
        // the flag it opens with is the only part that varies. Built on an Italian
        // thread inside MachineContract.English, as the refusal builds it. Safe to
        // write the thread cultures because the assembly disables test
        // parallelisation.
        var ui = CultureInfo.CurrentUICulture;
        var format = CultureInfo.CurrentCulture;
        var italian = CultureInfo.GetCultureInfo("it-IT");
        try
        {
            CultureInfo.CurrentUICulture = italian;
            CultureInfo.CurrentCulture = italian;

            var line = MachineContract.English(() => Program.AdminRightsNeededEventLogLine(arg));

            Assert.Equal(
                $"{arg} mode aborted: InstallerClean is not running as administrator, "
                + "so it did not scan. No action taken.",
                line);
        }
        finally
        {
            CultureInfo.CurrentUICulture = ui;
            CultureInfo.CurrentCulture = format;
        }
    }
}
