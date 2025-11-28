using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum CharacterState
{
    Idle,
    Move,
    Attack,
    Block,
    MoveBack
}

public class CharacterControl : MonoBehaviour
{
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
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float attackDamage = 20f; // Damage dealt per attack
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
     float attackRange = 4f; 
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
            NotifyOpponentOfAction();
        }
    }

    void HandleBlockInput()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            ChangeState(CharacterState.Block);
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

        // Calculate utility scores for each possible action
        Dictionary<CharacterState, float> utilities = new Dictionary<CharacterState, float>();

        utilities[CharacterState.Attack] = CalculateAttackUtility(distanceToPlayer, healthPercent);
        utilities[CharacterState.Move] = CalculateMoveTowardUtility(distanceToPlayer, healthPercent);
        utilities[CharacterState.MoveBack] = CalculateMoveAwayUtility(distanceToPlayer, healthPercent);
        utilities[CharacterState.Block] = 0f; CalculateBlockUtility(distanceToPlayer, healthPercent);
        utilities[CharacterState.Idle] = 0.1f;

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
        // 只有在攻击范围内才考虑攻击
        if (distance > attackRange) return 0f;

        // 基础效用值
        float utility = 1.0f;

        // 血量因子：血量越高越激进 (50%-100%的激进度)
        utility *= Mathf.Lerp(0.5f, 1.0f, healthPercent);

        // 距离因子：越近越好
        utility *= (attackRange - distance) / attackRange;

        // 在最佳攻击距离内给予额外奖励
        if (distance < attackRange * 0.7f)
        {
            utility *= 1.3f;
        }

        return utility;
    }

    float CalculateMoveTowardUtility(float distance, float healthPercent)
    {
        if (distance < attackRange + 1f) return 0f;

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
        if (distance < attackRange * 0.5f)
        {
            utility *= 2f;
        }

        return utility;
    }

    float CalculateBlockUtility(float distance, float healthPercent)
    {
        // Don't block if player is far away
        if (distance > attackRange * 1.5f) return 0f;

        float utility = 0.5f;
        utility *= (1f - healthPercent * 0.5f); // More defensive when damaged

        // Strongly prefer blocking if player is attacking
        if (playerControl != null && playerControl.GetCurrentState() == CharacterState.Attack)
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

        yield return new WaitForSeconds(attackDuration);

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
        if (currentState == CharacterState.Attack && !hasDealtDamageThisAttack)
        {
            // Try to get CharacterControl component from the collided object
            CharacterControl targetCharacter = collision.GetComponent<CharacterControl>();

            if (targetCharacter != null && targetCharacter != this)
            {
                // Check if target is blocking
                if (targetCharacter.GetCurrentState() == CharacterState.Block)
                {
                    // Blocked attack - deal reduced damage
                    targetCharacter.TakeDamage(attackDamage * 0.3f); // 30% damage when blocked
                    Debug.Log($"{gameObject.name} attack blocked by {targetCharacter.gameObject.name}! Reduced damage dealt.");
                }
                else
                {
                    // Normal attack - deal full damage
                    targetCharacter.TakeDamage(attackDamage);
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

        // You can add death animation, game over logic, etc. here
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

            yield return new WaitForSeconds(attackDuration / 2f);

            spriteRenderer.color = originalColor;
            isHurt = false;
            yield break;
        }

        yield return new WaitForSeconds(attackDuration / 2f);

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
        // 改用 WaitForEndOfFrame 确保 sprite 完全加载
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