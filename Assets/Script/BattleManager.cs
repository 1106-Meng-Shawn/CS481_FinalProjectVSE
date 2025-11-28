using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public static class InitData
{
    public static readonly Vector3 playerPosition = new Vector3(-7f, -1.25f, 0f);
    public static readonly Vector3 enemyPosition = new Vector3(7f, -1.25f, 0f);
    public static readonly Vector3 playerScale = new Vector3(1.5f, 1.5f, 0f);
    public static readonly Vector3 enemyScale = new Vector3(1.5f, 1.5f, 0f);
}

public class BattleManager : MonoBehaviour
{
    public CharacterControl PlayerPrefab;
    public CharacterControl EnemyPrefab;
    public PreviewCharacters CharactersLibrary;

    [SerializeField] Slider playerHealth;
    [SerializeField] Slider enemyHealth;

    CharacterControl player;
    CharacterControl enemy;

    public static BattleManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void InitBattle(PreviewCharacter playerCharacter, PreviewCharacter enemyCharacter)
    {
        this.player = Instantiate(PlayerPrefab, transform);
        this.player.SetCharacter(playerCharacter);

        this.enemy = Instantiate(EnemyPrefab, transform);
        this.enemy.SetCharacter(enemyCharacter);
        InitCharacterPosition();
        InitHealthUI();
    }

    void InitCharacterPosition()
    {
        player.gameObject.transform.position = InitData.playerPosition;
        player.gameObject.transform.localScale = InitData.playerScale;
        enemy.gameObject.transform.position = InitData.enemyPosition;
        enemy.gameObject.transform.localScale = InitData.enemyScale;
    }

    void InitHealthUI()
    {
        if (playerHealth != null && player != null)
        {
            playerHealth.maxValue = player.MaxHealth;
            playerHealth.value = player.CurrentHealth;
            player.OnHealthChanged += UpdatePlayerHealth;
            player.InitializeHealth();
        }

        if (enemyHealth != null && enemy != null)
        {
            enemyHealth.maxValue = enemy.MaxHealth;
            enemyHealth.value = enemy.CurrentHealth;

            enemy.OnHealthChanged += UpdateEnemyHealth;
            enemy.InitializeHealth();
        }
    }

    void UpdatePlayerHealth(float currentHealth, float maxHealth)
    {
        if (playerHealth != null)
        {
            playerHealth.value = currentHealth;
        }
    }

    void UpdateEnemyHealth(float currentHealth, float maxHealth)
    {
        if (enemyHealth != null)
        {
            enemyHealth.value = currentHealth;
        }
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnHealthChanged -= UpdatePlayerHealth;
        }

        if (enemy != null)
        {
            enemy.OnHealthChanged -= UpdateEnemyHealth;
        }
    }

    public CharacterControl GetPlayerCharacterControl()
    {
        return player;
    }
}