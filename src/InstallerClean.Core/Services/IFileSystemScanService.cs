using InstallerClean.Models;

namespace InstallerClean.Services;

/// <summary>
/// Walks <c>C:\Windows\Installer</c>, asks the Windows Installer API
/// which packages are registered, and returns a <see cref="ScanResult"/>
/// describing what is safe to clean up.
/// </summary>
/// <remarks>
/// The scan is the only authoritative source of orphan-vs-registered
/// truth in the system. Other components (<see cref="IMoveFilesService"/>,
/// <see cref="IDeleteFilesService"/>) operate on file lists derived
/// from a <see cref="ScanResult"/> and never re-classify. Callers
/// should not cache results across user-driven mutations: run a fresh
/// scan after every Move or Delete.
/// </remarks>
public interface IFileSystemScanService
{
    /// <summary>
    /// Run the scan. Reports progress via <paramref name="progress"/> as text and
    /// item counts, never as a percentage: milestone updates carry the fixed
    /// phase messages, and non-milestone updates carry the ticker along with the
    /// position the current phase has reached and, where the phase knows it, the
    /// total it is working towards (see <see cref="ScanProgressUpdate"/>). What
    /// share of a bar a phase is worth belongs to whatever draws the bar.
    ///
    /// Throws <see cref="LocalisedInvalidOperationException"/> wherever the scan
    /// stops rather than give an answer it has not established, with a message
    /// built from the app's own strings and safe to show. Among those stops: a
    /// listing of <c>C:\Windows\Installer</c> that ends in an error, whether
    /// Windows reports the folder is not there, refuses the listing, or fails a
    /// read part-way through it; and Windows Installer returning no registered
    /// products at all, which would otherwise read every cached file as unclaimed.
    /// Throws <see cref="UnauthorizedAccessException"/> if Windows refuses the
    /// process access to the Windows Installer records, and
    /// <see cref="OperationCanceledException"/> when
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    Task<ScanResult> ScanAsync(
        IProgress<ScanProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}
