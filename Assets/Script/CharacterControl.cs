using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum CharacterState
{
    Idle,
    Move,
    Attack,
    Attack2,
    Block,
    MoveBack
}

public class CharacterControl : MonoBehaviour
{
    private Timer timer;

    [Header("Character Setup")]
    PreviewCharacter previewCharacter;
    [SerializeField] bool isPlayer = false;
    [SerializeField] SpriteRenderer spriteRenderer;

    [Header("Physics")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;

    [Header("Combat")]
    [SerializeField] float attackDuration = 0.5f;
    [SerializeField] float blockDuration = 1f;
    [SerializeField] float attack2Duration = 0.25f; // Time until attack hits
    [SerializeField] float Attack2Damage = 5f; // Damage dealt by Attack2
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float attackDamage = 20f; // Damage dealt per attack
    [SerializeField] float hurtDuration = 0.125f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip attackSFX;
    [SerializeField] AudioClip attack2SFX;
    [SerializeField] AudioClip blockSFX;
    [SerializeField] AudioClip victorySFX;
    [SerializeField] AudioClip defeatSFX;

    private float currentHealth;
    public System.Action<float, float> OnHealthChanged;

    // Track if damage has been dealt in current attack to prevent multiple hits
    private bool hasDealtDamageThisAttack = false;

    public float CurrentHealth
    {
        get { return currentHealth; }
        private set
        {
            currentHealth = value;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    public float MaxHealth
    {
        get { return maxHealth; }
    }

    [Header("AI Settings")]
     float aiDecisionCooldown = 0.1f;
     float attackRange = 6.5f; 
     float safeDistance = 6f; 


    [Header("UI")]
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] float titleOffsetY = 0f;

    [Header("Colliders")]
    [SerializeField] private PolygonCollider2D physicsCollider; // For physical blocking (not a trigger)
    [SerializeField] private PolygonCollider2D attackCollider;  // For attack detection (is a trigger)

    private CharacterState currentState = CharacterState.Idle;
    private bool stateLocked = false;
    private bool isHurt = false;

    // AI specific
    private Transform playerTransform;
    private CharacterControl playerControl;
    private float lastDecisionTime;
    private bool needsNewDecision = true;

    void Awake()
    {
        currentHealth = maxHealth;

        timer = FindFirstObjectByType<Timer>();

        // Setup Rigidbody2D if not set in inspector
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
        }

        // Configure Rigidbody2D for 2D side-view combat
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f; // No gravity for 2D side view
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent rotation
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Better collision detection
        }

        // Setup colliders if not set in inspector
        SetupColliders();
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    void SetupColliders()
    {
        PolygonCollider2D[] colliders = GetComponents<PolygonCollider2D>();

        // If we don't have 2 colliders, create them
        if (colliders.Length < 2)
        {
            // Remove existing colliders
            foreach (var col in colliders)
            {
                Destroy(col);
            }

            // Create physics collider (not a trigger)
            physicsCollider = gameObject.AddComponent<PolygonCollider2D>();
            physicsCollider.isTrigger = false;

            // Create attack collider (is a trigger)
            attackCollider = gameObject.AddComponent<PolygonCollider2D>();
            attackCollider.isTrigger = true;
        }
        else
        {
            // Assign existing colliders
            physicsCollider = colliders[0];
            attackCollider = colliders[1];

            // Make sure settings are correct
            physicsCollider.isTrigger = false;
            attackCollider.isTrigger = true;
        }

    }

    private void Start()
    {
    }

    public void SetCharacter(PreviewCharacter previewCharacter)
    {
        this.previewCharacter = previewCharacter;
        if (previewCharacter == null) return;
        SetSprite(previewCharacter.Stand);
        ChangeState(CharacterState.Idle);
        InitializeHealth();

        // Initialize AI references if this is not the player
        if (!isPlayer)
        {
            playerTransform = BattleManager.Instance.GetPlayerCharacterControl().transform;
            playerControl = BattleManager.Instance.GetPlayerCharacterControl();
            Debug.Log($"Set Player playerTransform == null{playerTransform == null} and playerControl == null{playerControl == null}");
        }
    }

    void Update()
    {
        ForcePosition();
        UpdateTitlePosition();

        if (isPlayer)
        {
            HandlePlayerInput();
        }
        else
        {
            HandleAI();
        }
    }

    void FixedUpdate()
    {
        // Use FixedUpdate for physics-based movement
        if (rb != null && currentState != CharacterState.Attack && currentState != CharacterState.Block)
        {
            // Keep velocity at zero when not actively moving
            if (currentState == CharacterState.Idle)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    // ================= UI Update =================
    void UpdateTitlePosition()
    {
        if (title != null && spriteRenderer != null)
        {
            Vector3 spriteTop = transform.position + Vector3.up * (spriteRenderer.bounds.size.y / 2f);
            Vector3 titleWorldPos = spriteTop + Vector3.up * titleOffsetY;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(titleWorldPos);
            title.transform.position = screenPos;
        }
    }

    // ================= Player Control =================
    void HandlePlayerInput()
    {
        // Only allow movement when not attacking or blocking
        if (currentState != CharacterState.Attack && currentState != CharacterState.Block)
        {
            HandleMoveState();
        }

        HandleAttackInput();
        HandleBlockInput();
    }

    void HandleMoveState()
    {
        float h = 0;
        if (Input.GetKey(KeyCode.A)) h = -1;
        if (Input.GetKey(KeyCode.D)) h = 1;

        if (h != 0)
        {
            // Use Rigidbody2D for physics-based movement
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(h * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                transform.Translate(Vector2.right * h * moveSpeed * Time.deltaTime);
            }

            // Update flip state and colliders when direction changes
            bool newFlipState = h < 0;
            if (spriteRenderer.flipX != newFlipState)
            {
                spriteRenderer.flipX = newFlipState;
                UpdatePolygonColliders(); // Update colliders when flip changes
            }

            ChangeState(CharacterState.Move);
        }
        else
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            ChangeState(CharacterState.Idle);
        }
    }

    void HandleAttackInput()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            ChangeState(CharacterState.Attack);
            VolumeManager.Instance.PlaySFX(attackSFX);
            NotifyOpponentOfAction();
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            ChangeState(CharacterState.Attack2);
            VolumeManager.Instance.PlaySFX(attack2SFX);
            NotifyOpponentOfAction();
        }
    }

    void HandleBlockInput()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            ChangeState(CharacterState.Block);
            VolumeManager.Instance.PlaySFX(blockSFX);
        }
    }

    // ================= AI Control =================
    void HandleAI()
    {
        if (playerTransform == null) return;

        // Determine if AI should make a new decision
        bool shouldDecide = needsNewDecision || (Time.time - lastDecisionTime > aiDecisionCooldown);

        if (!stateLocked && shouldDecide)
        {
            MakeAIDecision();
            lastDecisionTime = Time.time;
            needsNewDecision = false;
        }

        ExecuteCurrentState();
    }

    void MakeAIDecision()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        float healthPercent = currentHealth / maxHealth;

        // --- REACTION LOGIC: AI immediately reacts to player's actions ---
        CharacterState playerState = playerControl.GetCurrentState();

        // 1. Player is attacking → AI tries to block
        if (playerState == CharacterState.Attack || playerState == CharacterState.Attack2 && Random.value < 0.5f)
        {
            // If close enough, block immediately
            if (distanceToPlayer < attackRange * 1.3f)
            {
                ChangeState(CharacterState.Block);
                needsNewDecision = true;
                return; // Skip normal decision-making
            }

            // If far away, AI moves back to avoid attack
            if (distanceToPlayer < safeDistance)
            {
                ChangeState(CharacterState.MoveBack);
                needsNewDecision = true;
                return;
            }
        }

        // Block more often when low HP
        if (healthPercent < 0.25f && distanceToPlayer < attackRange)
        {
            ChangeState(CharacterState.Block);
            needsNewDecision = true;
            return;
        }

        //attack logic for AI
        if (distanceToPlayer < attackRange * 1.0f)
        {
            if (Random.value < 0.5f)
            {
                ChangeState(CharacterState.Attack2);
            }
            else
            {
                ChangeState(CharacterState.Attack);
            }

            needsNewDecision = true;
            return; // Skip the utility system
        }

        // Calculate utility scores for each possible action
        Dictionary<CharacterState, float> utilities = new Dictionary<CharacterState, float>();

        utilities[CharacterState.Attack] = CalculateAttackUtility(distanceToPlayer, healthPercent);
        utilities[CharacterState.Attack2] = CalculateAttack2Utility(distanceToPlayer, healthPercent);
        utilities[CharacterState.Move] = CalculateMoveTowardUtility(distanceToPlayer, healthPercent);
        utilities[CharacterState.MoveBack] = CalculateMoveAwayUtility(distanceToPlayer, healthPercent);
        utilities[CharacterState.Block] = CalculateBlockUtility(distanceToPlayer, healthPercent);
        utilities[CharacterState.Idle] = 0.2f;

        // If player is attacking → increase block + retreat
        if (playerState == CharacterState.Attack || playerState == CharacterState.Attack2)
        {
            utilities[CharacterState.Block] *= 2f;
            utilities[CharacterState.MoveBack] *= 2f;
        }

        if (playerState == CharacterState.Attack && distanceToPlayer > attackRange)
        {
            utilities[CharacterState.Move] *= 2.5f;
            utilities[CharacterState.Attack] *= 1.3f;
            utilities[CharacterState.Attack2] *= 1.4f;
        }

        // Low HP, prioritize blocking (defensive)
        if (healthPercent < 0.3f)
        {
            utilities[CharacterState.Block] *= 2f;
            utilities[CharacterState.MoveBack] *= 2f;
            utilities[CharacterState.Attack] *= 0.6f;
        }

        if (distanceToPlayer > attackRange * 1f)
        {
            utilities[CharacterState.Move] += 1.2f;
        }

        // Debug output - 可以注释掉
        Debug.Log($"AI Decision - Distance: {distanceToPlayer:F2} and attak Range =={attackRange}, Health: {healthPercent:F2}%");
        Debug.Log($"Utilities - Attack: {utilities[CharacterState.Attack]:F2}, Move: {utilities[CharacterState.Move]:F2}, MoveBack: {utilities[CharacterState.MoveBack]:F2}, Block: {utilities[CharacterState.Block]:F2}");

        // Select action with highest utility
        CharacterState bestState = CharacterState.Idle;
        float maxUtility = 0f;

        foreach (var pair in utilities)
        {
            if (pair.Value > maxUtility)
            {
                maxUtility = pair.Value;
                bestState = pair.Key;
            }
        }

        Debug.Log($"AI Chosen: {bestState} (utility: {maxUtility:F2})");
        ChangeState(bestState);
    }

    float CalculateAttackUtility(float distance, float healthPercent)
    {
        if (distance > attackRange) return 0f;

        float utility = 1.0f;

        utility *= Mathf.Lerp(0.5f, 1.0f, healthPercent);

        //utility *= (attackRange - distance) / attackRange;

        utility *= Mathf.Clamp01((attackRange - distance * 0.7f) / attackRange);

        if (distance < attackRange * 0.7f)
        {
            utility *= 1.3f;
        }

        //if (playerControl != null && playerControl.GetCurrentState() != CharacterState.Block)
        //    utility *= 1.5f;

        return utility;
    }

    float CalculateAttack2Utility(float distance, float healthPercent)
    {
        if (distance > attackRange) return 0f;

        float utility = 0.9f;

        utility *= Mathf.Lerp(0.5f, 1.0f, healthPercent);

        //utility *= (attackRange - distance) / attackRange;

        utility *= Mathf.Clamp01((attackRange - distance * 0.7f) / attackRange);

        if (distance < attackRange * 0.7f)
        {
            utility *= 1.3f;
        }

        return utility;
    }

    float CalculateMoveTowardUtility(float distance, float healthPercent)
    {
        if (distance < attackRange * 1.2f) return 0f;

        float utility = 0.7f;
        utility *= healthPercent; // More willing to approach when healthy
        utility *= Mathf.Clamp01(distance / safeDistance);

        return utility;
    }


    float CalculateMoveAwayUtility(float distance, float healthPercent)
    {
        float utility = 0.6f;
        utility *= (1f - healthPercent); // More likely to retreat when low health
        utility *= Mathf.Clamp01(1f - distance / safeDistance);

        // Extra incentive to retreat if very close
        if (distance < attackRange * 0.8f)
        {
            utility += 0.3f;
        }

        utility *= Mathf.Clamp01((attackRange - distance) / attackRange);

        return utility;
    }

    float CalculateBlockUtility(float distance, float healthPercent)
    {
        // Don't block if player is far away
        if (distance > attackRange * 1.2f) return 0f;

        float utility = 0.5f;
        utility *= (1f - healthPercent * 0.4f); // More defensive when damaged

        // Strongly prefer blocking if player is attacking
        if (playerControl != null && playerControl.GetCurrentState() == CharacterState.Attack || playerControl.GetCurrentState() == CharacterState.Attack2)
        {
            utility *= 3f;
        }

        return utility;
    }

    // ================= State Execution =================
    void ExecuteCurrentState()
    {
        if (playerTransform == null) return;

        switch (currentState)
        {
            case CharacterState.Move:
                MoveTowardPlayer();
                break;
            case CharacterState.MoveBack:
                MoveAwayFromPlayer();
                break;
            case CharacterState.Idle:
                // Stop movement when idle
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
                break;
        }
    }

    void MoveTowardPlayer()
    {
        if (stateLocked) return;

        float direction = Mathf.Sign(playerTransform.position.x - transform.position.x);

        // Use Rigidbody2D for physics-based movement
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            transform.Translate(Vector2.right * direction * moveSpeed * Time.deltaTime);
        }

        // Update flip state and colliders when direction changes
        bool newFlipState = direction < 0;
        if (spriteRenderer.flipX != newFlipState)
        {
            spriteRenderer.flipX = newFlipState;
            UpdatePolygonColliders(); // Update colliders when flip changes
        }
    }

    void MoveAwayFromPlayer()
    {
        if (stateLocked) return;

        float direction = -Mathf.Sign(playerTransform.position.x - transform.position.x);

        // Use Rigidbody2D for physics-based movement
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            transform.Translate(Vector2.right * direction * moveSpeed * Time.deltaTime);
        }

        // Update flip state and colliders when direction changes
        bool newFlipState = direction < 0;
        if (spriteRenderer.flipX != newFlipState)
        {
            spriteRenderer.flipX = newFlipState;
            UpdatePolygonColliders(); // Update colliders when flip changes
        }
    }

    // ================= State Management =================
    void ChangeState(CharacterState newState)
    {
        if (stateLocked) return;
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case CharacterState.Idle:
                SetSprite(previewCharacter.Stand);
                break;
            case CharacterState.Move:
            case CharacterState.MoveBack:
                SetSprite(previewCharacter.Stand);
                break;
            case CharacterState.Attack:
                StartCoroutine(AttackState());
                break;
            case CharacterState.Attack2:
                StartCoroutine(Attack2State());
                break;
            case CharacterState.Block:
                StartCoroutine(BlockState());
                break;
        }
    }

    IEnumerator AttackState()
    {
        stateLocked = true;
        hasDealtDamageThisAttack = false; // Reset damage flag for this attack

        // Stop movement during attack
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        SetSprite(previewCharacter.Attack0);

        VolumeManager.Instance.PlaySFX(attackSFX);

        yield return new WaitForSeconds(attackDuration);

        stateLocked = false;
        needsNewDecision = true;
        ChangeState(CharacterState.Idle);
    }

    IEnumerator Attack2State()
    {
        stateLocked = true;
        hasDealtDamageThisAttack = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        SetSprite(previewCharacter.Attack2);

        VolumeManager.Instance.PlaySFX(attack2SFX);

        yield return new WaitForSeconds(attack2Duration);

        stateLocked = false;
        needsNewDecision = true;
        ChangeState(CharacterState.Idle);
    }
    IEnumerator BlockState()
    {
        stateLocked = true;

        // Stop movement during block
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        SetSprite(previewCharacter.Stand);

        VolumeManager.Instance.PlaySFX(blockSFX);

        yield return new WaitForSeconds(blockDuration);

        stateLocked = false;
        needsNewDecision = true;
        ChangeState(CharacterState.Idle);
    }

    // ================= Collision Detection =================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only the attack collider (trigger) will call this
        // Check if this character is in attack state
        if ((currentState == CharacterState.Attack || currentState == CharacterState.Attack2) && !hasDealtDamageThisAttack)
        {
            // Try to get CharacterControl component from the collided object
            CharacterControl targetCharacter = collision.GetComponent<CharacterControl>();

            if (targetCharacter != null && targetCharacter != this)
            {
                float damageToDeal = (currentState == CharacterState.Attack2)
                              ? Attack2Damage          // Light attack damage
                              : attackDamage;           // Heavy attack damage

                // Check if target is blocking
                if (targetCharacter.GetCurrentState() == CharacterState.Block)
                {
                    // Blocked attack - deal reduced damage
                    VolumeManager.Instance.PlaySFX(blockSFX);
                    targetCharacter.TakeDamage(attackDamage * 0.3f); // 30% damage when blocked
                    Debug.Log($"{gameObject.name} attack blocked by {targetCharacter.gameObject.name}! Reduced damage dealt.");
                }
                else
                {
                    // Normal attack - deal full damage
                    targetCharacter.TakeDamage(damageToDeal);
                    Debug.Log($"{gameObject.name} hit {targetCharacter.gameObject.name} for {attackDamage} damage!");
                }

                // Mark that damage has been dealt in this attack
                hasDealtDamageThisAttack = true;
            }
        }
    }

    // ================= Damage System =================
    public void TakeDamage(float damage)
    {
        // Reduce current health by damage amount
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(0, CurrentHealth);

        Debug.Log($"{gameObject.name} took {damage} damage. Current health: {CurrentHealth}/{maxHealth}");

        // Trigger hurt state visual feedback
        if (!isHurt)
        {
            StartCoroutine(HurtState());
        }

        // Make AI reconsider its strategy after taking damage
        if (!isPlayer)
        {
            needsNewDecision = true;
        }

        // Check for death
        if (CurrentHealth <= 0)
        {
            OnCharacterDeath();
        }
    }

    private void OnCharacterDeath()
    {
        // Handle character death
        Debug.Log($"{gameObject.name} has been defeated!");

        // Stop all movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // Disable physics
        }

        // Disable controls
        stateLocked = true;

        // Show lose to the defeated character
        if (previewCharacter != null && previewCharacter.Lose != null)
        {
            SetSprite(previewCharacter.Lose);
        }

        // If player loses, then show enemy win animation
        if (isPlayer)
        {
            VolumeManager.Instance.swapMusic(VolumeManager.Instance.bgmVictory, false);
            var enemy = BattleManager.Instance.GetEnemyCharacterControl();
            if (enemy != null && enemy.previewCharacter.Win != null)
            {
                enemy.SetSprite(enemy.previewCharacter.Win);
            }
        }

        //if enemy loses, then show player win animation
        else
        {
            VolumeManager.Instance.swapMusic(VolumeManager.Instance.bgmDefeat, false);
            var player = BattleManager.Instance.GetPlayerCharacterControl();
            if (player != null && player.previewCharacter.Win != null)
            {
                player.SetSprite(player.previewCharacter.Win);
            }
        }

        // You can add death animation, game over logic, etc. here
        if (timer != null)
        {
           timer.CheckInstantWin();
        }
        // Example: Destroy(gameObject, 2f); // Destroy after 2 seconds
    }

    public void InitializeHealth()
    {
        CurrentHealth = maxHealth;
    }

    IEnumerator HurtState()
    {
        isHurt = true;

        Sprite originalSprite = spriteRenderer.sprite;

        // Use hit sprite if available, otherwise flash red
        if (previewCharacter.Hit != null)
        {
            SetSprite(previewCharacter.Hit);
        }
        else
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;

            yield return new WaitForSeconds(hurtDuration / 2f);

            spriteRenderer.color = originalColor;
            isHurt = false;
            yield break;
        }

        yield return new WaitForSeconds(hurtDuration / 2f);

        SetSprite(originalSprite);
        isHurt = false;
    }

    // ================= Helper Methods =================
    void ForcePosition()
    {
        // Lock character to ground level and prevent rotation
        transform.position = new Vector3(transform.position.x, -1.25f, transform.position.z);
        transform.rotation = Quaternion.identity;
    }

    void SetSprite(Sprite sprite)
    {
        if (sprite == null) return;
        StartCoroutine(SetSpriteCoroutine(sprite));
    }

    IEnumerator SetSpriteCoroutine(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
        yield return new WaitForEndOfFrame();
        UpdatePolygonColliders();
    }

    void UpdatePolygonColliders()
    {
        if (spriteRenderer.sprite == null) return;

        Sprite sprite = spriteRenderer.sprite;

        // Determine flip multiplier based on sprite flip state
        float flipMultiplier = spriteRenderer.flipX ? -1f : 1f;

        // Update physics collider (not a trigger) - used for physical blocking
        if (physicsCollider != null)
        {
            physicsCollider.pathCount = sprite.GetPhysicsShapeCount();
            List<Vector2> path = new List<Vector2>();

            for (int i = 0; i < physicsCollider.pathCount; i++)
            {
                path.Clear();
                sprite.GetPhysicsShape(i, path);

                // Flip the collider path if sprite is flipped
                for (int j = 0; j < path.Count; j++)
                {
                    path[j] = new Vector2(path[j].x * flipMultiplier * 0.7f, path[j].y * 0.7f);
                }

                physicsCollider.SetPath(i, path);
            }
        }

        // Update attack collider (is a trigger) - used for attack detection
        if (attackCollider != null)
        {
            attackCollider.pathCount = sprite.GetPhysicsShapeCount();
            List<Vector2> path = new List<Vector2>();

            for (int i = 0; i < attackCollider.pathCount; i++)
            {
                path.Clear();
                sprite.GetPhysicsShape(i, path);

                // Make attack collider slightly larger for better hit detection
                // AND flip it if sprite is flipped
                for (int j = 0; j < path.Count; j++)
                {
                    path[j] = new Vector2(path[j].x * flipMultiplier * 1.6f, path[j].y * 1.6f);
                }

                attackCollider.SetPath(i, path);
            }
        }
    }

    public CharacterState GetCurrentState()
    {
        return currentState;
    }

    public void NotifyOpponentOfAction()
    {
        // Notify opponent to reconsider strategy
        if (!isPlayer) needsNewDecision = true;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize attack range (red) and safe distance (yellow) in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, safeDistance);
    }
}