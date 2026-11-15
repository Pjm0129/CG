using StarterAssets;
using UnityEngine;

public class GameSettingsBootstrap : MonoBehaviour
{
    private void Awake()
    {
        GameSettings.Apply();

        StarterAssetsInputs inputs =
            FindFirstObjectByType<StarterAssetsInputs>();

        if (inputs == null)
            return;

        MouseSensitivityApplier applier =
            inputs.GetComponent<MouseSensitivityApplier>();

        if (applier == null)
            inputs.gameObject.AddComponent<MouseSensitivityApplier>();
    }
}
