namespace Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;

/// <summary>
/// Thrown when a permanent-deletion request targets a feature or application that is not currently
/// in <c>PendingDeletion</c> state. This happens when the entity was restored by a usage event
/// between the moment the user viewed the pending-delete list and the moment they confirmed.
/// </summary>
public sealed class FeatureNotPendingDeletionException(string message) : FeatureSwitcherException(message, 3);
