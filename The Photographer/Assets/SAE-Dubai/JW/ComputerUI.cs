using TMPro;
using UnityEngine;

public class ComputerUI : MonoBehaviour
{
    public float currentMoney = 100f;
    public TMP_Text moneyText;
    public GameObject PlayerObject;
    public GameObject ComputerCamera;

    void Start()
    {
        PlayerObject.SetActive(false);
        ComputerCamera.SetActive(true);
        Cursor.lockState = CursorLockMode.Confined;
    }

    // Update is called once per frame
    void Update()
    {
        moneyText.text = $"Money: ${currentMoney}";

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (ComputerCamera.activeSelf)
            {
                ComputerCamera.SetActive(false);
                PlayerObject.SetActive(true);
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            else
            {
                ComputerCamera.SetActive(true);
                PlayerObject.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

            }
        }
    }

    public bool AttemptBuy(float cost)
    {
        if (currentMoney >= cost)
        {
            currentMoney -= cost;
            return true;
        }
        else
        {
            return false;
        }
    }
}
