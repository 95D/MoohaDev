using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Headwind.Mooha.Editor.Bake
{
    /// <summary>
    /// Keeps the baked size table matched to the active build target.
    /// <para>Texture bytes move with compression format and max size platform overrides, so a table
    /// baked for one target is wrong for another. Rather than carrying per-target entries, the bake
    /// runs at every moment where the target may have changed or is about to be consumed, which
    /// leaves one set of numbers that is always the current target's.</para>
    /// <para>A build is the one point where a stale table would ship, so a failed bake fails the
    /// build instead of warning.</para>
    /// </summary>
    public sealed class ResourceSizeTableBakeHooks
        : IActiveBuildTargetChanged,
            IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        [MenuItem("Tools/Mooha/Bake Resource Size Table")]
        public static void BakeNow() => ResourceSizeTableBaker.Bake();

        [InitializeOnLoadMethod]
        private static void InstallPlayModeHook()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                ResourceSizeTableBaker.Bake();
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget) =>
            ResourceSizeTableBaker.Bake();

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!ResourceSizeTableBaker.Bake())
            {
                throw new BuildFailedException(
                    "[Mooha] Resource size table bake failed; see the preceding console error."
                );
            }
        }
    }
}
