using TMPro;
using UnityEngine;

public class BuffButton : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    private Buff currentBuff;
    private GameManager gameManager;

    public void Setup(Buff buff, GameManager manager)
    {
        currentBuff = buff;
        gameManager = manager;
        nameText.text = buff.buffName;
    }

    public void OnClick()
    {
        gameManager.ApplyBuff(currentBuff);
    }
}
