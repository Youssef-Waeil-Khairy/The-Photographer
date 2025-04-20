using UnityEngine;

namespace SAE_Dubai.JW.UI
{
    public class BuyButton : MonoBehaviour
    {
        public CameraInfoPanel cameraInfoPanel;

        public void AttemptBuy()
        {
            if (cameraInfoPanel == null)
            {
                return;
            }

            if (cameraInfoPanel.CamSettings == null)
            {
                return;
            }

            if (cameraInfoPanel.CameraPrice > PlayerBalance.Instance.Balance)
            {
                return;
            }
            // TODO: Add checks to not allow the same camera to be bought multiple times
            
            PlayerBalance.Instance.Balance -= cameraInfoPanel.CameraPrice;
            cameraInfoPanel.CameraItemPrefab.SetActive(true);
        }
    }
}