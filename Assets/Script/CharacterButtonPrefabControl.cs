using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButtonPrefabControl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] Button button;
    [SerializeField] Image CharacterIcon;
    PreviewCharacter previewCharacter;
    void Start()
    {
        InitButtons();
    }


    void InitButtons()
    {
        button.onClick.AddListener(OnButtonClick);
    }


    void OnButtonClick()
    {
        StartPanelManager.Instance.SetCurrentCharacter(previewCharacter);
    }


    public void Init(PreviewCharacter previewCharacter)
    {
        this.previewCharacter = previewCharacter;
        CharacterIcon.sprite = previewCharacter.Portrait;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
