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
    bool isSettingsOpen = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        PlayButton.onClick.AddListener(Play);
        SettingsButton.onClick.AddListener(Settings);
        BackButton.onClick.AddListener(Back);
        QuitButton.onClick.AddListener(Quit);

        mainMenu.SetActive(true); //should show only the main menu
        SettingsPanel.SetActive(false);
    }
    void Play()
    {
        SceneManager.LoadScene("BattleScene");
    }

    void Settings()
    {
        SettingsPanel.SetActive(true);
        mainMenu.SetActive(false);
    }

    void Back()
    {
        SettingsPanel.SetActive(false);
        mainMenu.SetActive(true);
    }

    void Quit()
    {
        Application.Quit();
    }
}
