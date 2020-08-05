using UnityEngine;
using UnityEngine.UI;

public class TurretEnergyBar : MonoBehaviour
{
    [Tooltip("Health component to track")] public EnemyTurret turret;

    [Tooltip("Image component displaying health left")]
    public Image energyBar;

    [Tooltip("The floating healthbar pivot transform")]
    public Transform energyBarPivot;

    [Tooltip("Whether the health bar is visible when at full health or not")]
    public bool hideFullHealthBar = true;

    public bool lookAtCamera = false;

    void Update()
    {
        // update health bar value
        energyBar.fillAmount = turret.currentEnergy / turret.maxEnergy;

        if (lookAtCamera)
            // rotate health bar to face the camera/player
            energyBarPivot.LookAt(Camera.main.transform.position);

        // hide health bar if needed
        if (hideFullHealthBar)
            energyBarPivot.gameObject.SetActive(energyBar.fillAmount != 1);
    }
}