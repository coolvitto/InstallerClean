using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// A <see cref="MockFileSystem"/> whose folder listings fail, either when the listing
/// is asked for or after it has given every entry the folder holds. The first is where
/// .NET reports a folder Windows refuses to list, because it opens the folder as it
/// builds the enumeration. The second stands in for a read failing part-way through.
///
/// MockFileSystem models no permissions and no failing read, so without this double
/// neither condition reaches the scan's walk under test.
/// </summary>
internal sealed class FailingListingFileSystem : MockFileSystem
{
    private readonly FailingDirectoryInfoFactory _directoryInfo;

    /// <param name="failure">What the listing throws.</param>
    /// <param name="afterEntries">
    /// False to throw when the listing is asked for; true to give every entry first.
    /// </param>
    internal FailingListingFileSystem(Exception failure, bool afterEntries) =>
        _directoryInfo = new FailingDirectoryInfoFactory(this, failure, afterEntries);

    public override IDirectoryInfoFactory DirectoryInfo => _directoryInfo;

    private sealed class FailingDirectoryInfoFactory(
        FailingListingFileSystem fs, Exception failure, bool afterEntries) : IDirectoryInfoFactory
    {
        public IFileSystem FileSystem => fs;

        public IDirectoryInfo New(string path) =>
            new FailingDirectoryInfo(fs, path, failure, afterEntries);

        public IDirectoryInfo? Wrap(DirectoryInfo? directoryInfo) =>
            directoryInfo is null ? null : New(directoryInfo.FullName);
    }

    private sealed class FailingDirectoryInfo(
        FailingListingFileSystem fs, string path, Exception failure, bool afterEntries)
        : MockDirectoryInfo(fs, path)
    {
        public override IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
        {
            if (!afterEntries) throw failure;
            return ThenFail(base.EnumerateFiles(searchPattern, searchOption));
        }

        private IEnumerable<IFileInfo> ThenFail(IEnumerable<IFileInfo> listed)
        {
            foreach (var entry in listed) yield return entry;
            throw failure;
        }
    }
}
