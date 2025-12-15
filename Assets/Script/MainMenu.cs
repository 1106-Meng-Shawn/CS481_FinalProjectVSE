using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject SettingsPanel;
    public GameObject creditsPanel;
    public GameObject mainMenu;
    [SerializeField] public Button SettingsButton;
    [SerializeField] public Button PlayButton;
    [SerializeField] public Button QuitButton;
    [SerializeField] public Button CreditsButton;
    [SerializeField] public Button BackButton;
    [SerializeField] public Button CreditsBackButton;
    [SerializeField] public Slider musicSlider;
    [SerializeField] public Slider sfxSlider;
    bool isSettingsOpen = false;
    bool isCreditsOpen = false; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        PlayButton.onClick.AddListener(Play);
        SettingsButton.onClick.AddListener(Settings);
        BackButton.onClick.AddListener(Back);
        QuitButton.onClick.AddListener(Quit);
        CreditsButton.onClick.AddListener(Credits);
        CreditsBackButton.onClick.AddListener(CreditsBack);
        musicSlider.onValueChanged.AddListener(musicChange);
        sfxSlider.onValueChanged.AddListener(sfxChange);

        mainMenu.SetActive(true); //should show only the main menu
        SettingsPanel.SetActive(false);
    }
    void Play()
    {
        VolumeManager.Instance.PlayButtonClick();
        SceneManager.LoadScene("BattleScene");
    }

    void Settings()
    {
        VolumeManager.Instance.PlayButtonClick();
        SettingsPanel.SetActive(true);
        mainMenu.SetActive(false);
    }

    void Back()
    {
        VolumeManager.Instance.PlayButtonClick();
        SettingsPanel.SetActive(false);
        mainMenu.SetActive(true);
    }

    void CreditsBack()
    {
        VolumeManager.Instance.PlayButtonClick();
        creditsPanel.SetActive(false);
        mainMenu.SetActive(true);
    }
    void Quit()
    {
        VolumeManager.Instance.PlayButtonClick();
        Application.Quit();
    }

    void Credits()
    {
        VolumeManager.Instance.PlayButtonClick();
        SettingsPanel.SetActive(false);
        mainMenu.SetActive(false);
        creditsPanel.SetActive(true);
    }
    void musicChange(float value)
    {
        VolumeManager.Instance?.SetMusicVolume(value);
    }

    void sfxChange(float value)
    {
        VolumeManager.Instance?.SetSFXVolume(value);
    }
}
