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
            }
            else
            {
                ComputerCamera.SetActive(true);
                PlayerObject.SetActive(false);
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
