using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject SettingsPanel;
    public GameObject mainMenu;
    [SerializeField] public Button SettingsButton;
    [SerializeField] public Button PlayButton;
    [SerializeField] public Button QuitButton;
    [SerializeField] public Button BackButton;
    [SerializeField] public Slider musicSlider;
    [SerializeField] public Slider sfxSlider;
    bool isSettingsOpen = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        PlayButton.onClick.AddListener(Play);
        SettingsButton.onClick.AddListener(Settings);
        BackButton.onClick.AddListener(Back);
        QuitButton.onClick.AddListener(Quit);
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

    void Quit()
    {
        VolumeManager.Instance.PlayButtonClick();
        Application.Quit();
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
