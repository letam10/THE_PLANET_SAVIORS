using UnityEditor;
using UnityEngine;
using TPS.Runtime.Core;

namespace TPS.Editor
{
    internal static class PhaseContentAuthoringTools
    {
        [MenuItem("Tools/TPS/Content/Install Or Update Aster Harbor Proof Content")]
        private static void InstallOrUpdateAsterHarborProofContent()
        {
            Phase1SceneInstaller.InstallVerticalSlice();
            Debug.Log("[TPSContent] Reinstalled Aster Harbor proof content and updated shared catalog.");
        }

        [MenuItem("Tools/TPS/Content/Install And Audit Proof Content")]
        private static void InstallAndAuditProofContent()
        {
            Phase1ProjectAudit.ReinstallAndAuditMenu();
        }

        [MenuItem("Tools/TPS/Content/Run Content Validation")]
        private static void RunContentValidation()
        {
            ContentValidationResult result = PhaseContentValidator.ValidateSharedCatalogAsset();
            if (result.Errors.Count == 0 && result.Warnings.Count == 0)
            {
                Debug.Log("[TPSContent] Content validation passed.");
                return;
            }

            for (int i = 0; i < result.Warnings.Count; i++)
            {
                Debug.LogWarning($"[TPSContent] {result.Warnings[i]}");
            }

            for (int i = 0; i < result.Errors.Count; i++)
            {
                Debug.LogError($"[TPSContent] {result.Errors[i]}");
            }
        }
    }
}
