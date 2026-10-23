using UnityEngine;
using TMPro;

public class LorePopupUI : MonoBehaviour
{
    public static LorePopupUI Instance { get; private set; }

    [Header("UI Elemanları")]
    [SerializeField] private GameObject promptBox;         // [E] İncele kutusu
    [SerializeField] private TextMeshProUGUI promptText;

    [SerializeField] private GameObject lorePanel;         // Parşömen paneli
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideAll();
    }

    public void ShowPrompt(string text)
    {
        if (lorePanel != null && lorePanel.activeSelf) return;
        if (promptText != null) promptText.text = text;
        if (promptBox != null) promptBox.SetActive(true);
    }

    public void OpenPanel(string title, string body)
    {
        if (promptBox != null) promptBox.SetActive(false);
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (lorePanel != null) lorePanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (lorePanel != null) lorePanel.SetActive(false);
    }

    public void HideAll()
    {
        if (promptBox != null) promptBox.SetActive(false);
        if (lorePanel != null) lorePanel.SetActive(false);
    }

    public bool IsOpen()
    {
        return lorePanel != null && lorePanel.activeSelf;
    }
}
