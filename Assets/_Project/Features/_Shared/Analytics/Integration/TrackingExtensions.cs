using Cysharp.Threading.Tasks;
using Ezg.Tracking;

/// <summary>
///     Game-side forwarding extensions that bind this project's strongly-typed event enums to the generic
///     <see cref="TrackingService" /> engine. Declared in the global namespace so existing call sites
///     (e.g. <c>FirebaseEvent.button_click.Send(config)</c>) keep working without any new using directive.
/// </summary>
public static class TrackingExtensions
{
    /// <summary>
    ///     Sends a Firebase event using this project's <see cref="FirebaseEvent" /> enum and config.
    /// </summary>
    /// <param name="eventName">The Firebase event to send.</param>
    /// <param name="config">The optional configuration parameters for the event.</param>
    /// <returns>A UniTask representing the asynchronous operation.</returns>
    public static UniTask Send(this FirebaseEvent eventName, FirebaseEventConfig config = null)
    {
        return TrackingService.SendFirebase(eventName.ToString(), config);
    }

    /// <summary>
    ///     Sends an AppsFlyer event using this project's <see cref="AppFlyerEvent" /> enum and config.
    /// </summary>
    /// <param name="eventName">The AppsFlyer event to send.</param>
    /// <param name="config">The optional configuration parameters for the event.</param>
    public static void Send(this AppFlyerEvent eventName, AppflyerEventConfig config = null)
    {
        TrackingService.SendAppsFlyer(eventName.ToString(), config);
    }
}