using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


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
        BattleManager.Instance.InitBattle(playerSel.currentCharacter, enemySel.currentCharacter);
        panel.SetActive(false);
    }


    void OnEndButtonClick()
    {
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
    }

    void InitEnemySel(PreviewCharacter currentCharacter)
    {
        enemySel.currentCharacter = currentCharacter;
        enemySel.characterIcon.sprite = currentCharacter.Portrait;
        enemySel.SelectButton.onClick.AddListener(() => OnSelectButtonClick(false));
        enemySel.RandomButton.onClick.AddListener(() => OnRandomButtonClick(enemySel));
    }



    void OnSelectButtonClick(bool isPlayer)
    {
        RefreshScrollRect(isPlayer);
    }

    void OnRandomButtonClick(CharacterSel sel)
    {
        PreviewCharacter newChar = PreviewCharacters.Instance.GetRandomPreviewCharacter();
        SetCurrentCharacter(sel.isPlayer, newChar);
    }

    void SetCurrentCharacter(bool isPlayer ,PreviewCharacter newCharacter)
    {
        if (isPlayer)
        {
            playerSel.currentCharacter = newCharacter;
            playerSel.characterIcon.sprite = newCharacter.Portrait;
        }
        else
        {
            enemySel.currentCharacter = newCharacter;
            enemySel.characterIcon.sprite = newCharacter.Portrait;

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
