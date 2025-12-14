using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum CharacterState
{
    Idle,
    Move,
    Attack,
    Attack1,
    Attack2,
    Block,
    MoveBack,
    Heal,
    Jump
}

public abstract class CharacterControl : MonoBehaviour
{
    protected Timer timer;

    [Header("Character Setup")]
    protected PreviewCharacter previewCharacter;
    [SerializeField] protected SpriteRenderer spriteRenderer;

    [Header("Physics")]
    [SerializeField] protected Rigidbody2D rb;

    protected float currentHealth;
    public System.Action<float, float> OnHealthChanged;
    protected bool hasDealtDamageThisAttack = false;

    public float CurrentHealth
    {
        get { return currentHealth; }
        protected set
        {
            currentHealth = value;
            OnHealthChanged?.Invoke(currentHealth, previewCharacter.maxHealth);
        }
    }

    public float MaxHealth => previewCharacter.maxHealth;

    [Header("UI")]
    [SerializeField] protected TextMeshProUGUI title;
    [SerializeField] protected float titleOffsetY = 0f;

    [Header("Colliders")]
    [SerializeField] protected PolygonCollider2D physicsCollider;

    protected CharacterState currentState = CharacterState.Idle;
    protected bool stateLocked = false;
    protected bool isHurt = false;

    protected bool canJump = true;
    protected bool canHeal = true;

    protected bool isJumping = false;
    protected bool isGrounded = true;

    protected float healingProgress = 0f;
    protected bool isHealing = false;

    private int groundContactCount = 0;

    public virtual void EnableJump(bool enable) => canJump = enable;
    public virtual void EnableHeal(bool enable) => canHeal = enable;

    public virtual void PerformJump()
    {
        if (canJump && isGrounded)
        {
            ChangeState(CharacterState.Jump);
        }
    }

    public virtual void StartHealing()
    {
        if (!canHeal || stateLocked) return;

        isHealing = true;

        if (currentState != CharacterState.Heal)
        {
            ChangeState(CharacterState.Heal);
        }
    }

    public virtual void StopHealing()
    {
        isHealing = false;

        if (currentState == CharacterState.Heal)
        {
            ChangeState(CharacterState.Idle);
        }
    }

    protected void ResetHealing()
    {
        isHealing = false;
        healingProgress = 0f;
    }

    public virtual void TryJumpAttack()
    {
        if (!isGrounded && Random.value < previewCharacter.jumpAttackChance)
        {
            ChangeState(CharacterState.Jump);
        }
    }

    protected virtual IEnumerator JumpState()
    {
        isGrounded = false;
        isJumping = true;
        groundContactCount = 0;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, previewCharacter.jumpForce);
        }

        SetSprite(previewCharacter.Stand);

        while (!isGrounded)
        {
            yield return null;
        }

        OnStateComplete();
        ChangeState(CharacterState.Idle);
    }

    protected virtual bool IsAttacking()
    {
        return currentState == CharacterState.Attack ||
               currentState == CharacterState.Attack1 ||
               currentState == CharacterState.Attack2;
    }

    protected virtual void Awake()
    {
        timer = FindFirstObjectByType<Timer>();

        SetupRigidbody();
        SetupColliders();
    }

    protected virtual void Start()
    {
        currentHealth = previewCharacter.maxHealth;
    }

    protected virtual void Update()
    {
        ForcePosition();
        UpdateTitlePosition();
        HandleControl();
    }

    protected virtual void FixedUpdate()
    {
        if (rb != null &&
            currentState != CharacterState.Attack &&
            currentState != CharacterState.Block &&
            currentState != CharacterState.Heal &&
            currentState != CharacterState.Jump)
        {
            if (currentState == CharacterState.Idle)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
    }

    protected abstract void HandleControl();

    protected void SetupRigidbody()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }
    }

    protected void SetupColliders()
    {
        PolygonCollider2D[] colliders = GetComponents<PolygonCollider2D>();

        foreach (var col in colliders)
        {
            Destroy(col);
        }

        physicsCollider = gameObject.AddComponent<PolygonCollider2D>();
        physicsCollider.isTrigger = false;
    }

    public virtual void SetCharacter(PreviewCharacter previewCharacter)
    {
        this.previewCharacter = previewCharacter;
        if (previewCharacter == null) return;
        SetSprite(previewCharacter.Stand);
        ChangeState(CharacterState.Idle);
        InitializeHealth();
    }

    public void InitializeHealth()
    {
        CurrentHealth = previewCharacter.maxHealth;
    }

    public CharacterState GetCurrentState()
    {
        return currentState;
    }

    public void NotifyOpponentOfAction()
    {
        OnOpponentAction();
    }

    protected virtual void OnOpponentAction()
    {
    }

    public void ShowVictory()
    {
        if (previewCharacter != null && previewCharacter.Win != null)
        {
            SetSprite(previewCharacter.Win);
        }
    }

    protected void Move(float direction)
    {
        if (stateLocked) return;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * previewCharacter.moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            transform.Translate(Vector2.right * direction * previewCharacter.moveSpeed * Time.deltaTime);
        }

        bool newFlipState = direction < 0;
        if (spriteRenderer.flipX != newFlipState)
        {
            spriteRenderer.flipX = newFlipState;
            UpdatePolygonColliders();
        }
    }

    protected void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    public void PerformAttack()
    {
        ChangeState(CharacterState.Attack);
        VolumeManager.Instance.PlaySFX(previewCharacter.attackSFX);
    }

    public void PerformAttack1()
    {
        ChangeState(CharacterState.Attack1);
        VolumeManager.Instance.PlaySFX(previewCharacter.attack1SFX);
    }

    public void PerformAttack2()
    {
        ChangeState(CharacterState.Attack2);
        VolumeManager.Instance.PlaySFX(previewCharacter.attack2SFX);
    }

    public void PerformBlock()
    {
        ChangeState(CharacterState.Block);
        VolumeManager.Instance.PlaySFX(previewCharacter.blockSFX);
    }

    public void Heal()
    {
        if (CurrentHealth < previewCharacter.maxHealth)
        {
            CurrentHealth += previewCharacter.HealAmount;
            CurrentHealth = Mathf.Min(CurrentHealth, previewCharacter.maxHealth);
        }
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(0, CurrentHealth);

        Debug.Log($"{gameObject.name} took {damage} damage. Current health: {CurrentHealth}/{previewCharacter.maxHealth}");

        if (!isHurt)
        {
            StartCoroutine(HurtState());
        }

        OnTakeDamage();

        if (CurrentHealth <= 0)
        {
            OnCharacterDeath();
        }
    }

    protected virtual void OnTakeDamage()
    {
    }

    protected virtual void ChangeState(CharacterState newState)
    {
        if (stateLocked && currentState != CharacterState.Block) return;
        if (currentState == newState) return;

        if (currentState == CharacterState.Heal && newState != CharacterState.Heal)
        {
            isHealing = false;
        }

        currentState = newState;

        switch (currentState)
        {
            case CharacterState.Idle:
                SetSprite(previewCharacter.Stand);
                break;

            case CharacterState.Attack:
                StartCoroutine(AttackState());
                break;

            case CharacterState.Attack1:
                StartCoroutine(Attack1State());
                break;

            case CharacterState.Attack2:
                StartCoroutine(Attack2State());
                break;

            case CharacterState.Block:
                StartCoroutine(BlockState());
                break;

            case CharacterState.Heal:
                StartCoroutine(HealState());
                break;

            case CharacterState.Jump:
                StartCoroutine(JumpState());
                break;
        }
    }

    protected IEnumerator AttackState()
    {
        stateLocked = true;
        hasDealtDamageThisAttack = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        SetSprite(previewCharacter.Attack0);
        VolumeManager.Instance.playHit1();
        yield return new WaitForSeconds(previewCharacter.attackDuration);

        stateLocked = false;
        OnStateComplete();
        ChangeState(CharacterState.Idle);
    }

    protected IEnumerator Attack1State()
    {
        stateLocked = true;
        hasDealtDamageThisAttack = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        SetSprite(previewCharacter.Attack1);
        VolumeManager.Instance.playHit2();

        yield return new WaitForSeconds(previewCharacter.attack1Duration);

        stateLocked = false;
        OnStateComplete();
        ChangeState(CharacterState.Idle);
    }

    protected IEnumerator Attack2State()
    {
        stateLocked = true;
        hasDealtDamageThisAttack = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        SetSprite(previewCharacter.Attack2);
        VolumeManager.Instance.playHit3();

        yield return new WaitForSeconds(previewCharacter.attack2Duration);

        stateLocked = false;
        OnStateComplete();
        ChangeState(CharacterState.Idle);
    }

    protected IEnumerator BlockState()
    {
        stateLocked = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        SetSprite(previewCharacter.Groggy);
        VolumeManager.Instance.PlaySFX(previewCharacter.blockSFX);

        while (currentState == CharacterState.Block)
        {
            yield return null;
        }

        stateLocked = false;
    }

    protected IEnumerator HealState()
    {
        stateLocked = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (previewCharacter.Win != null)
            SetSprite(previewCharacter.Win);
        else
            SetSprite(previewCharacter.Stand);


        while (isHealing && CurrentHealth < previewCharacter.maxHealth)
        {
            float timer = 0f;
            while (timer < previewCharacter.HealDuration)
            {
                if (!isHealing || currentState != CharacterState.Heal)
                {
                    stateLocked = false;
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            Heal();
        }

        stateLocked = false;
        OnStateComplete();
        ChangeState(CharacterState.Idle);
    }

    protected virtual void OnStateComplete()
    {
    }

    protected IEnumerator HurtState()
    {
        isHurt = true;

        if (isHealing)
        {
            ResetHealing();
            ChangeState(CharacterState.Idle);
        }

        Sprite originalSprite = spriteRenderer.sprite;

        if (previewCharacter.Hit != null)
        {
            SetSprite(previewCharacter.Hit);
        }
        else
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;

            yield return new WaitForSeconds(previewCharacter.hurtDuration / 2f);

            spriteRenderer.color = originalColor;
            isHurt = false;
            yield break;
        }

        yield return new WaitForSeconds(previewCharacter.hurtDuration / 2f);

        SetSprite(originalSprite);
        isHurt = false;
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsAttacking() && !hasDealtDamageThisAttack)
        {
            CharacterControl targetCharacter = collision.gameObject.GetComponent<CharacterControl>();

            if (targetCharacter != null && targetCharacter != this)
            {
                float damageToDeal = GetCurrentAttackDamage();

                if (targetCharacter.GetCurrentState() == CharacterState.Block)
                {
                    VolumeManager.Instance.playBlock();
                    targetCharacter.TakeDamage(damageToDeal * previewCharacter.blockReduction);
                    Debug.Log($"{gameObject.name} attack blocked by {targetCharacter.gameObject.name}! Reduced damage dealt.");
                }
                else
                {
                    targetCharacter.TakeDamage(damageToDeal);
                    Debug.Log($"{gameObject.name} hit {targetCharacter.gameObject.name} for {damageToDeal} damage!");
                }

                hasDealtDamageThisAttack = true;
                return;
            }
        }

        CharacterControl otherCharacter = collision.gameObject.GetComponent<CharacterControl>();
        bool isLandingOnCharacter = false;

        if (otherCharacter != null && otherCharacter != this)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    isLandingOnCharacter = true;
                    break;
                }
            }
        }

        bool isGroundCollision = otherCharacter == null || isLandingOnCharacter;

        if (isGroundCollision)
        {
            groundContactCount++;
            if (groundContactCount > 0 && !isGrounded)
            {
                isGrounded = true;
                isJumping = false;
            }
        }
    }

    protected virtual void OnCollisionExit2D(Collision2D collision)
    {
        CharacterControl otherCharacter = collision.gameObject.GetComponent<CharacterControl>();
        bool wasLandingOnCharacter = false;

        if (otherCharacter != null && otherCharacter != this)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    wasLandingOnCharacter = true;
                    break;
                }
            }
        }

        bool wasGroundCollision = otherCharacter == null || wasLandingOnCharacter;

        if (wasGroundCollision)
        {
            groundContactCount--;
            if (groundContactCount <= 0)
            {
                groundContactCount = 0;
            }
        }
    }

    protected float GetCurrentAttackDamage()
    {
        switch (currentState)
        {
            case CharacterState.Attack:
                return previewCharacter.AttackDamage;
            case CharacterState.Attack1:
                return previewCharacter.Attack1Damage;
            case CharacterState.Attack2:
                return previewCharacter.Attack2Damage;
            default:
                return 0f;
        }
    }

    protected virtual void OnCharacterDeath()
    {
        Debug.Log($"{gameObject.name} has been defeated!");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        stateLocked = true;

        VolumeManager.Instance.playKO();
        if (previewCharacter != null && previewCharacter.Lose != null)
        {
            SetSprite(previewCharacter.Lose);
        }

        if (timer != null)
        {
            timer.CheckInstantWin();
        }
    }

    protected void UpdateTitlePosition()
    {
        if (title != null && spriteRenderer != null)
        {
            Vector3 spriteTop = transform.position + Vector3.up * (spriteRenderer.bounds.size.y / 2f);
            Vector3 titleWorldPos = spriteTop + Vector3.up * titleOffsetY;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(titleWorldPos);
            title.transform.position = screenPos;
        }
    }

    protected void ForcePosition()
    {
        transform.rotation = Quaternion.identity;
    }

    protected void SetSprite(Sprite sprite)
    {
        if (sprite == null) return;
        StartCoroutine(SetSpriteCoroutine(sprite));
    }

    protected IEnumerator SetSpriteCoroutine(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
        yield return new WaitForEndOfFrame();
        UpdatePolygonColliders();
    }

    protected void UpdatePolygonColliders()
    {
        if (spriteRenderer.sprite == null) return;

        Sprite sprite = spriteRenderer.sprite;
        float flipMultiplier = spriteRenderer.flipX ? -1f : 1f;

        if (physicsCollider != null)
        {
            physicsCollider.pathCount = sprite.GetPhysicsShapeCount();
            List<Vector2> path = new List<Vector2>();

            for (int i = 0; i < physicsCollider.pathCount; i++)
            {
                path.Clear();
                sprite.GetPhysicsShape(i, path);

                for (int j = 0; j < path.Count; j++)
                {
                    path[j] = new Vector2(path[j].x * flipMultiplier * 1f, path[j].y * 1f);
                }

                physicsCollider.SetPath(i, path);
            }
        }
    }

    protected void PlaySFX(AudioClip clip)
    {
        if (clip != null && previewCharacter.audioSource != null)
        {
            previewCharacter.audioSource.PlayOneShot(clip);
        }
    }
}