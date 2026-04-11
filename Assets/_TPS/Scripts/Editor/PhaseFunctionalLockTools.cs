using UnityEditor;
using UnityEngine;

namespace TPS.Editor
{
    internal static class PhaseFunctionalLockTools
    {
        [MenuItem("Tools/TPS/Functional Lock/Run Compile & Audit")]
        private static void RunCompileAndAudit()
        {
            AssetDatabase.Refresh();

            ContentValidationResult validation = PhaseContentValidator.ValidateSharedCatalogAsset();
            LogValidation(validation);
            PhaseEnvironmentTools.ValidateReplaceSafeLayoutMenu();
            Phase1ProjectAudit.RunProjectAuditMenu();
            Debug.Log("[TPSFunctionalLock] Compile & Audit complete. Continue with Assets/_TPS/Docs/FINAL_VERIFICATION_PACK.md");
        }

        [MenuItem("Tools/TPS/Functional Lock/Prepare Final Core Smoke")]
        private static void PrepareFinalCoreSmoke()
        {
            Phase1ProjectAudit.PrepareManualSmoke();
            Debug.Log("[TPSFunctionalLock] Final core smoke prepared. Follow Assets/_TPS/Docs/FINAL_VERIFICATION_PACK.md");
        }

        [MenuItem("Tools/TPS/Functional Lock/Run Final Validation Pack")]
        private static void RunFinalValidationPack()
        {
            AssetDatabase.Refresh();
            Phase1SceneInstaller.InstallVerticalSlice();
            ContentValidationResult validation = PhaseContentValidator.ValidateSharedCatalogAsset();
            LogValidation(validation);
            PhaseEnvironmentTools.ValidateReplaceSafeLayoutMenu();
            Phase1ProjectAudit.ReinstallAndAuditMenu();
            Debug.Log("[TPSFunctionalLock] Validation pack complete. Review Assets/_TPS/Docs/FINAL_VERIFICATION_PACK.md for remaining smoke and reopen proof steps.");
        }

        private static void LogValidation(ContentValidationResult validation)
        {
            if (validation == null)
            {
                Debug.LogError("[TPSFunctionalLock] Content validation failed to run.");
                return;
            }

            for (int i = 0; i < validation.Warnings.Count; i++)
            {
                Debug.LogWarning($"[TPSFunctionalLock] {validation.Warnings[i]}");
            }

            for (int i = 0; i < validation.Errors.Count; i++)
            {
                Debug.LogError($"[TPSFunctionalLock] {validation.Errors[i]}");
            }

            if (validation.Errors.Count == 0 && validation.Warnings.Count == 0)
            {
                Debug.Log("[TPSFunctionalLock] Content validation passed clean.");
            }
        }
    }
}
