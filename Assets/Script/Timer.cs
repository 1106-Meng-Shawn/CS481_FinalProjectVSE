using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public GameObject winScreen;
    public TextMeshProUGUI winText;
    public TextMeshProUGUI loseText;
    public TextMeshProUGUI drawText;
    private float startTime;
    private float maxTime = 60f; // 60 seconds
    private bool isRunning = false;

    [SerializeField] public Button restartButton;
    [SerializeField] public Button MainMenuButton;

    void Start()
    {
        winScreen.SetActive(false);
        startTime = maxTime;
        isRunning = false;   // do NOT start counting
        UpdateTimer();
    }

    private void Awake()
    {
        restartButton.onClick.AddListener(RestartGame);
        MainMenuButton.onClick.AddListener(MainMenu);
    }
    public void StartTimer()
    {
        startTime = maxTime;
        isRunning = true;
    }
    void Update()
    {
        if(!isRunning) return;

        if(Time.timeScale == 0f) return; // Pause timer when game is paused

        startTime -= Time.deltaTime;

        if(startTime <= 0)
        {
            startTime = 0;
            isRunning = false;
            EndGame();
            // Timer has finished, you can add additional logic here if needed
        }
        UpdateTimer();
    }

    void UpdateTimer()
    {
        timerText.text = Mathf.FloorToInt(startTime).ToString();
    }

    void RestartTimer()
    {
        startTime = maxTime;
        isRunning = true;
        UpdateTimer();
    }
    void EndGame()
    {
        CharacterControl player = BattleManager.Instance.GetPlayerCharacterControl();
        CharacterControl enemy = BattleManager.Instance.GetEnemyCharacterControl();

        if (player.CurrentHealth > enemy.CurrentHealth)
        {
            Win();
        }
        else if (player.CurrentHealth < enemy.CurrentHealth)
        {
            EnemyWin();
        }
        else
        {
            Debug.Log("It's a draw!");
        }
    }

    void Win()
    {
        CharacterControl player = BattleManager.Instance.GetPlayerCharacterControl();
        CharacterControl enemy = BattleManager.Instance.GetEnemyCharacterControl();

        if (player.CurrentHealth > enemy.CurrentHealth || enemy.CurrentHealth <= 0)
        {
            Time.timeScale = 0f;
            winScreen.SetActive(true);

            winText.gameObject.SetActive(true);
            loseText.gameObject.SetActive(false);
            drawText.gameObject.SetActive(false);

            winText.text = "Player Wins!";
        }
    }

    public void CheckInstantWin()
    {
        CharacterControl player = BattleManager.Instance.GetPlayerCharacterControl();
        CharacterControl enemy = BattleManager.Instance.GetEnemyCharacterControl();

        // If someone is dead, evaluate who wins
        if (player.CurrentHealth <= 0)
        {
            EnemyWin();
        }
        else if (enemy.CurrentHealth <= 0)
        {
            Win();
        }
    }

    void EnemyWin()
    {
        CharacterControl player = BattleManager.Instance.GetPlayerCharacterControl();
        CharacterControl enemy = BattleManager.Instance.GetEnemyCharacterControl();

        if(enemy.CurrentHealth > player.CurrentHealth || player.CurrentHealth <= 0)
        {
            Time.timeScale = 0f;
            winScreen.SetActive(true);

            winText.gameObject.SetActive(false);
            loseText.gameObject.SetActive(true);
            drawText.gameObject.SetActive(false);

            loseText.text = "Enemy Wins!";
        }
    }

    private void Draw()
    {
        CharacterControl player = BattleManager.Instance.GetPlayerCharacterControl();
        CharacterControl enemy = BattleManager.Instance.GetEnemyCharacterControl();

        if(player.CurrentHealth == enemy.CurrentHealth)
        {
            Time.timeScale = 0f;
            winScreen.SetActive(true);

            winText.gameObject.SetActive(false);
            loseText.gameObject.SetActive(false);
            drawText.gameObject.SetActive(true);

            drawText.text = "It's a Draw!";
        }
    }

    void RestartGame()
    {
        Time.timeScale = 1f; // Resume time scale
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    void MainMenu()
    {
        Time.timeScale = 1f; // Resume time scale
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
