using Ezg.Core.RedDot;
using UnityEngine;

namespace Ezg.Feature.RedDot
{
    /// <summary>
    ///     Project-side red-dot indicator. Resolves the listened event key from the
    ///     game-specific <see cref="RedDotId" /> enum so the reusable <see cref="BaseRedDot" />
    ///     package stays free of project data.
    /// </summary>
    public abstract class RedDotBadge : BaseRedDot
    {
        #region Fields

        [SerializeField] private RedDotId _notifId;

        /// <inheritdoc />
        protected override string EventKey => _notifId.ToString();

        #endregion
    }
}