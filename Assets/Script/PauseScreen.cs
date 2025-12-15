using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseScreen : MonoBehaviour
{
    public GameObject pauseScreen;
    [SerializeField] public Button ResumeButton;
    [SerializeField] public Button RestartButton;
    [SerializeField] public Button MainMenuButton;
    bool isPaused = false;
    bool isTopPanelActive = false;



    void Awake()
    {
        ResumeButton.onClick.AddListener(ResumeGame);
        RestartButton.onClick.AddListener(RestartGame);
        MainMenuButton.onClick.AddListener(MainMenu);
        pauseScreen.SetActive(false);
        isPaused = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) //press escape to pause the game
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void ResumeGame()
    {
        VolumeManager.Instance.Muffle(false);
        pauseScreen.SetActive(false); //deactivates the pauseScreen and unpauses the screen
        Time.timeScale = 1f; //game starts running again
        isPaused = false;
    }

    void PauseGame()
    {
        VolumeManager.Instance.Muffle(true);
        pauseScreen.SetActive(true); //activates the pauseScreen and pauses the screen
        Time.timeScale = 0f; //game stops running.
        isPaused = true;
    }

    void RestartGame()
    {
        VolumeManager.Instance.Muffle(false);
        Time.timeScale = 1f; // Resume time scale
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void MainMenu()
    {
        VolumeManager.Instance.Muffle(false);
        SceneManager.LoadScene("MainMenu");
    }
}
