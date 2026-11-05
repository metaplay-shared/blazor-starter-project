using Metaplay.Core.Config;

namespace Game.Logic
{
    /// <summary>
    /// Registry for all game configuration data. Should be used for the game's economy data, entity data (units,
    /// buildings, etc.), in-app definitions, localization metadata (which languages are supported), etc.
    /// <para>
    /// Every entry is built from the CSV sheet of the same name in <c>GameConfigSource/</c>, and that naming is
    /// the mechanism rather than a convention: the build asks its source for one sheet per declared entry, so
    /// adding an entry here means adding <c>GameConfigSource/&lt;EntryName&gt;.csv</c> beside it. Use
    /// <c>GameConfigLibrary&lt;TKey, TInfo&gt;</c> for item libraries and <c>GameConfigKeyValue</c> for
    /// single-instance tunables.
    /// </para>
    /// </summary>
    public class SharedGameConfig : SharedGameConfigBase
    {
        /// <summary>
        /// Global, singleton game configuration values (game-wide tunables).
        /// </summary>
        [GameConfigEntry("Global")]
        public GlobalConfig Global { get; private set; } = new GlobalConfig();

        // TODO: Example content, replace with your own. Delete this entry and its sheet together.
        /// <summary>
        /// The auto-clicker types a player can buy. Example item library; its rows are authored in
        /// <c>GameConfigSource/AutoClickers.csv</c>.
        /// </summary>
        [GameConfigEntry("AutoClickers")]
        public GameConfigLibrary<AutoClickerId, AutoClickerInfo> AutoClickers { get; private set; }

        /// <summary>
        /// Runs the content rules the build must prove, reporting each failure against the sheet and row that
        /// caused it. An error here stops the archive from being written.
        /// <para>
        /// <b>This is where a new feature plugs in.</b> A check belongs here wherever the failure it catches
        /// would otherwise show up as a valid-looking screen — an empty library that reads as a feature
        /// switched off, a pointer at a row that was deleted, a price nothing can afford. Those are the
        /// failures nothing at run time reports.
        /// </para>
        /// <para>
        /// Messages go to the SDK's <see cref="GameConfigValidationResult"/>, which carries the sheet, the row
        /// and the member into the build report the LiveOps Dashboard renders. Note that the SDK treats those
        /// messages as a report rather than a failure: the build succeeds and hands back an archive with the
        /// error inside it, so refusing to write it is the build tool's own gate
        /// (<c>tools/GameConfigGen</c>).
        /// </para>
        /// </summary>
        public override void BuildTimeValidate(GameConfigValidationResult validationResult)
        {
            base.BuildTimeValidate(validationResult);

            // TODO: Example content, replace with your own. The override is a real extension point; the check
            // in it belongs to the auto-clicker example.
            if (Global.ClicksPerTap < 1)
            {
                validationResult.Error("Global", nameof(GlobalConfig.ClicksPerTap),
                    $"ClicksPerTap is {Global.ClicksPerTap}, so a click would award nothing. It must be at least 1.",
                    columnHint: "Value");
            }
        }
    }
}
