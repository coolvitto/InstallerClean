using InstallerClean.Services;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What <see cref="InstallerCacheHelpers.ResolveFinalPathOutcome(string, out string, IFinalPathKernel)"/>
/// answers for each thing the kernel can say about the ancestor it opens: a refused
/// open, no final name on the first read or on the resized retry, a call that throws,
/// and a name that needs more room on the retry than the retry was given.
/// </summary>
/// <remarks>
/// The walk up to the ancestor is real. The path names a file not yet created, in a
/// folder made under the temp folder, and is spelled with a "." segment, so the walk
/// opens that folder and <see cref="Path.GetFullPath(string)"/> spells the path
/// differently from the way it was passed in. Every failure hands back that
/// GetFullPath spelling.
/// </remarks>
public sealed class InstallerCacheHelpersKernelAnswerTests : IDisposable
{
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "installerclean-kernel-answer-" + Guid.NewGuid().ToString("N"));

    private readonly string _path;

    public InstallerCacheHelpersKernelAnswerTests()
    {
        Directory.CreateDirectory(_folder);
        _path = Path.Combine(_folder, ".", "not-yet-created.msi");
    }

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch { }
    }

    [Fact]
    public void A_name_read_on_the_first_call_resolves()
    {
        // The control for every test below: the same walk, a handle and a name, and
        // the path comes back resolved as the name the kernel gave, with the part of
        // the path below the opened folder put back on it. The name is another
        // folder's, so it cannot be mistaken for the spelling the walk started from.
        var answered = Path.Combine(Path.GetTempPath(), "answered-by-the-kernel");
        var kernel = new ScriptedKernel((buffer, length) => Write(buffer, answered));

        var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(_path, out var resolved, kernel);

        Assert.Equal(PathResolution.Resolved, outcome);
        Assert.Equal(Path.Combine(answered, "not-yet-created.msi"), resolved);
        Assert.Equal(Path.GetFullPath(_folder), kernel.OpenedOn);
    }

    [Fact]
    public void A_refused_open_is_OpenRefused()
    {
        var kernel = new ScriptedKernel(refuseOpen: true);

        var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(_path, out var resolved, kernel);

        Assert.Equal(PathResolution.OpenRefused, outcome);
        Assert.Equal(Path.GetFullPath(_path), resolved);
        Assert.Equal(Path.GetFullPath(_folder), kernel.OpenedOn);
        Assert.Equal(0, kernel.NameReads);
    }

    [Fact]
    public void No_name_on_the_first_read_is_FinalNameUnavailable()
    {
        var kernel = new ScriptedKernel((buffer, length) => 0);

        var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(_path, out var resolved, kernel);

        Assert.Equal(PathResolution.FinalNameUnavailable, outcome);
        Assert.Equal(Path.GetFullPath(_path), resolved);
        Assert.Equal(1, kernel.NameReads);
    }

    [Fact]
    public void No_name_on_the_resized_retry_is_FinalNameUnavailable()
    {
        // The first read asks for more room than the buffer has, and the read made
        // with that room answers zero.
        var kernel = new ScriptedKernel(
            (buffer, length) => length + 100,
            (buffer, length) => 0);

        var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(_path, out var resolved, kernel);

        Assert.Equal(PathResolution.FinalNameUnavailable, outcome);
        Assert.Equal(Path.GetFullPath(_path), resolved);
        Assert.Equal(2, kernel.NameReads);
    }

    [Fact]
    public void An_open_that_throws_is_Faulted()
    {
        var kernel = new ScriptedKernel(throwOnOpen: true);

        var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(_path, out var resolved, kernel);

        Assert.Equal(PathResolution.Faulted, outcome);
        Assert.Equal(Path.GetFullPath(_path), resolved);
    }

    [Fact]
    public void A_name_read_that_throws_is_Faulted()
    {
        var kernel = new ScriptedKernel((buffer, length) => throw new IOException("scripted"));

        var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(_path, out var resolved, kernel);

        Assert.Equal(PathResolution.Faulted, outcome);
        Assert.Equal(Path.GetFullPath(_path), resolved);
    }

    [Fact]
    public void A_name_that_needs_more_room_again_on_the_retry_is_Faulted()
    {
        // The name grew between the two reads: the retry reports a length longer than
        // the buffer it was given, so no string can be read out of that buffer.
        var kernel = new ScriptedKernel(
            (buffer, length) => length + 100,
            (buffer, length) => length + 100);

        var outcome = InstallerCacheHelpers.ResolveFinalPathOutcome(_path, out var resolved, kernel);

        Assert.Equal(PathResolution.Faulted, outcome);
        Assert.Equal(Path.GetFullPath(_path), resolved);
        Assert.Equal(2, kernel.NameReads);
    }

    private static uint Write(char[] buffer, string name)
    {
        name.CopyTo(0, buffer, 0, name.Length);
        return (uint)name.Length;
    }

    /// <summary>
    /// Answers the open as told and each name read with the next of
    /// <c>reads</c>. The handles it hands out are not owned, so disposing one closes
    /// nothing. -1 is invalid on every platform and 1 is valid on every platform.
    /// </summary>
    private sealed class ScriptedKernel(
        bool refuseOpen = false,
        bool throwOnOpen = false,
        params Func<char[], uint, uint>[] reads) : IFinalPathKernel
    {
        public ScriptedKernel(params Func<char[], uint, uint>[] reads)
            : this(false, false, reads) { }

        public string? OpenedOn { get; private set; }

        public int NameReads { get; private set; }

        public SafeFileHandle Open(string path)
        {
            OpenedOn = path;
            if (throwOnOpen) throw new IOException("scripted");
            return new SafeFileHandle(new IntPtr(refuseOpen ? -1 : 1), ownsHandle: false);
        }

        public uint GetFinalName(SafeFileHandle handle, char[] buffer, uint length) =>
            reads[NameReads++](buffer, length);
    }
}
