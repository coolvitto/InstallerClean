namespace InstallerClean.Models;

/// <summary>
/// One installation of a product that one enumeration established: a row the product
/// walk listed, or an installation the recovery by name found for a product the walk
/// did not return. Everything <c>MsiEnumProductsEx</c> answers about an installation,
/// and so everything needed to recognise it again in a later answer.
/// </summary>
/// <param name="ProductCode">The product code as the enumeration returned it.</param>
/// <param name="UserSid">
/// The account the installation belongs to, or null for a per-machine installation.
/// </param>
/// <param name="Context">
/// The <c>MSIINSTALLCONTEXT</c> the installation is in (1 per-user managed, 2 per-user
/// unmanaged, 4 per-machine), carried as the raw API value for the reason
/// <see cref="PatchClaim.Context"/> is.
/// </param>
/// <param name="SecondCopyNotRuledOut">
/// Whether the enumeration's own reading of the installation's <c>InstanceType</c>
/// failed to show it to be an ordinary installation: the reading was positive, a second
/// instance of the product under an instance transform, or it did not read. Set where
/// that reading is taken, for the row it is taken for.
///
/// NO DEFAULT, SO NO ROW IS BUILT WITHOUT IT. The declared-product check compares every
/// installation package with the packages each such installation opens, and a row
/// that took false by omission would let a candidate through beside a second copy
/// nothing compared it with.
/// </param>
public sealed record ListedInstallation(
    string ProductCode,
    string? UserSid,
    int Context,
    bool SecondCopyNotRuledOut);
