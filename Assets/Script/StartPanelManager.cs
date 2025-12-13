using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartPanelManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static StartPanelManager Instance { get; private set; }
    [SerializeField] GameObject panel;

    [SerializeField] public CharacterSel playerSel;
    [SerializeField] public CharacterSel enemySel;

    [Serializable]
    public struct CharacterSel
    {
        public PreviewCharacter currentCharacter;
        public Image characterIcon;
        public Button SelectButton;
        public Button RandomButton;
        public bool isPlayer;
        public TextMeshProUGUI characterValueText;

    }

    [SerializeField] ScrollRect scrollRect;
    [SerializeField] CharacterButtonPrefabControl prefab;
    [SerializeField] Button StartButton;
    [SerializeField] Button EndButton;

    bool isPlayerSel = true;


    [SerializeField] public BackgroundButtons backgroundButtons;

    [Serializable]
    public struct BackgroundButtons
    {
        public Button GrasslandButton;
        public Button SnowfieldButton;
        public Button DesertButton;

    }

    public TextMeshProUGUI operatingInstructionsText;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }


    void Start()
    {
        InitStartPanel();

    }


    void InitStartPanel()
    {
        InitSel();
        InitButtons();
        SetScrollRectButtons();
        panel.SetActive(true);
        SetOperatingInstructionsText();
    }

    void SetOperatingInstructionsText()
    {
        operatingInstructionsText.text =
            $"<b>Controls:</b>\n" +
            $"- Move Left: <color=#00FFFF>{PlayerControl.playerLeftMove}</color>\n" +
            $"- Move Right: <color=#00FFFF>{PlayerControl.playerRightMove}</color>\n" +
            $"- Attack 0: <color=#FF0000>{PlayerControl.playerAttack0}</color>\n" +
            $"- Attack 1: <color=#FF4500>{PlayerControl.playerAttack1}</color>\n" +
            $"- Attack 2: <color=#FF6347>{PlayerControl.playerAttack2}</color>\n" +
            $"- Block: <color=#32CD32>{PlayerControl.playerBlock}</color>\n" +
            $"- Heal: <color=#00FF00>{PlayerControl.playerHeal}</color>\n" +
            $"- Jump: <color=#1E90FF>{PlayerControl.playerJump}</color>";
    }

    void InitSel()
    {

        PreviewCharacter playerChar = PreviewCharacters.Instance.GetRandomPreviewCharacter();
        InitCharacterSel(playerChar, true);
        PreviewCharacter enemy = PreviewCharacters.Instance.GetRandomPreviewCharacter();
        InitCharacterSel(enemy,false);

    }

    void InitButtons()
    {
        StartButton.onClick.AddListener(OnStartButtonClick);
        EndButton.onClick.AddListener(OnEndButtonClick);

        InitBackgroundButtons();
    }

    void InitBackgroundButtons()
    {
        backgroundButtons.GrasslandButton.onClick.AddListener(() => BackgroundManager.Instance.SetBackground(BackgroundType.Grassland));
        backgroundButtons.DesertButton.onClick.AddListener(() => BackgroundManager.Instance.SetBackground(BackgroundType.Desert));
        backgroundButtons.SnowfieldButton.onClick.AddListener(()=>BackgroundManager.Instance.SetBackground(BackgroundType.Snowfield));
    }


    void OnStartButtonClick()
    {
        VolumeManager.Instance.PlayButtonClick();
        BattleManager.Instance.InitBattle(playerSel.currentCharacter, enemySel.currentCharacter);
        panel.SetActive(false);
    }


    void OnEndButtonClick()
    {
       VolumeManager.Instance.PlayButtonClick();
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }


    void InitCharacterSel(PreviewCharacter currentCharacter,bool isPlayer)
    {
        if (isPlayer)
        {
            InitPlayerSel(currentCharacter);
        } else
        {
            InitEnemySel(currentCharacter);
        }
    }



    void InitPlayerSel(PreviewCharacter currentCharacter)
    {
        playerSel.currentCharacter = currentCharacter;
        playerSel.characterIcon.sprite = currentCharacter.Portrait;
        playerSel.SelectButton.onClick.AddListener(() => OnSelectButtonClick(true));
        playerSel.RandomButton.onClick.AddListener(() => OnRandomButtonClick(playerSel));
        SetDamageInfoText(true);

    }

    void InitEnemySel(PreviewCharacter currentCharacter)
    {
        enemySel.currentCharacter = currentCharacter;
        enemySel.characterIcon.sprite = currentCharacter.Portrait;
        enemySel.SelectButton.onClick.AddListener(() => OnSelectButtonClick(false));
        enemySel.RandomButton.onClick.AddListener(() => OnRandomButtonClick(enemySel));
        SetDamageInfoText(false);
    }


    void SetDamageInfoText(bool isPlayer)
    {
        TextMeshProUGUI valueText;
        PreviewCharacter newCharacter;

        if (isPlayer)
        {
            valueText = playerSel.characterValueText;
            newCharacter = playerSel.currentCharacter;
            BattleManager.Instance.UpdatePlayerHealth(newCharacter.maxHealth, newCharacter.maxHealth);

        }
        else
        {
            valueText = enemySel.characterValueText;
            newCharacter = enemySel.currentCharacter;
            BattleManager.Instance.UpdateEnemyHealth(newCharacter.maxHealth, newCharacter.maxHealth);

        }


        valueText.text =
            $"<b>Damage Information:</b>\n" +
            $"- Move Speed: <color=#00FFFF>{newCharacter.moveSpeed}</color>\n" +
            $"- Attack 0: <color=#FF0000>{newCharacter.AttackDamage} HP</color> (Duration: <color=#FFA500>{newCharacter.attackDuration}s</color>)\n" +
            $"- Attack 1: <color=#FF4500>{newCharacter.Attack1Damage} HP</color> (Duration: <color=#FFA500>{newCharacter.attack1Duration}s</color>)\n" +
            $"- Attack 2: <color=#FF6347>{newCharacter.Attack2Damage} HP</color> (Duration: <color=#FFA500>{newCharacter.attack2Duration}s</color>)\n" +
            $"- Block: Reduces incoming damage by <color=#32CD32>{(newCharacter.blockReduction * 100).ToString("N0")}%</color> (Duration: <color=#FFA500>{newCharacter.blockDuration}s</color>)\n" +
            $"- Heal: Restores <color=#00FF00>{newCharacter.HealAmount} HP</color> (Duration: <color=#FFA500>{newCharacter.HealDuration}s</color>)\n" +
            //$"- Max Health: <color=#FF69B4>{newCharacter.maxHealth}</color>\n" +
            $"- Jump Force: <color=#1E90FF>{newCharacter.jumpForce}</color>";



    }


    void OnSelectButtonClick(bool isPlayer)
    {
        VolumeManager.Instance.PlayButtonClick();
        RefreshScrollRect(isPlayer);
    }

    void OnRandomButtonClick(CharacterSel sel)
    {
        PreviewCharacter newChar = PreviewCharacters.Instance.GetRandomPreviewCharacter();
        SetCurrentCharacter(sel.isPlayer, newChar);
    }

    void SetCurrentCharacter(bool isPlayer ,PreviewCharacter newCharacter)
    {
        VolumeManager.Instance.PlayCharaClick();
        if (isPlayer)
        {
            playerSel.currentCharacter = newCharacter;
            playerSel.characterIcon.sprite = newCharacter.Portrait;
            SetDamageInfoText(true);
        }
        else
        {
            enemySel.currentCharacter = newCharacter;
            enemySel.characterIcon.sprite = newCharacter.Portrait;
            SetDamageInfoText(false);


        }
    }


    public void SetCurrentCharacter(PreviewCharacter newCharacter)
    {
        SetCurrentCharacter(isPlayerSel, newCharacter);
    }




    void RefreshScrollRect(bool isPlayer)
    {
        isPlayerSel = isPlayer;
        Vector3 Scale = scrollRect.gameObject.transform.localScale;
        Vector3 playerScale = new Vector3(Math.Abs(Scale.x), Scale.y, Scale.z);
        Vector3 enemyScale = new Vector3(-Math.Abs(Scale.x), Scale.y, Scale.z);
        scrollRect.gameObject.transform.localScale = isPlayer ? playerScale : enemyScale;
    }

    void SetScrollRectButtons()
    {
        List<PreviewCharacter> previewCharacters = PreviewCharacters.Instance.GetAllPreviewCharacters();

        RectTransform content = scrollRect.content;

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        foreach (var character in previewCharacters)
        {
            CharacterButtonPrefabControl item =
                Instantiate(prefab, content);

            item.Init(character);
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
