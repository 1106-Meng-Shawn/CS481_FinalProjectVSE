using System.Collections.Generic;
using UnityEngine;

public class CharacterControl_Enemy : CharacterControl
{
    [Header("AI Settings")]
    [SerializeField] private float aiDecisionCooldown = 0.1f;
    [SerializeField] private float attackRange = 6.5f;
    [SerializeField] private float safeDistance = 6f;

    private Transform playerTransform;
    private CharacterControl playerControl;
    private float lastDecisionTime;
    private bool needsNewDecision = true;

    private bool aiHoldingBlock = false;
    private float blockHoldEndTime = 0f;

    public override void SetCharacter(PreviewCharacter previewCharacter)
    {
        base.SetCharacter(previewCharacter);
        playerTransform = BattleManager.Instance.GetPlayerCharacterControl().transform;
        playerControl = BattleManager.Instance.GetPlayerCharacterControl();
    }

    protected override void HandleControl()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (currentState == CharacterState.Heal)
        {
            bool shouldStopHealing = distanceToPlayer <= attackRange;

            if (shouldStopHealing)
            {
                StopHealing();
                needsNewDecision = true;
            }
            else
            {
                return;
            }
        }

        if (aiHoldingBlock)
        {
            if (Time.time < blockHoldEndTime)
            {
                return;
            }
            else
            {
                StopAIBlock();
                needsNewDecision = true;
            }
        }

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
        float healthPercent = currentHealth / previewCharacter.maxHealth;
        CharacterState playerState = playerControl.GetCurrentState();

        Dictionary<CharacterState, float> utilities = new Dictionary<CharacterState, float>();

        // 只计算"攻击"这个大类的效用
        utilities[CharacterState.Attack] = 0f;
        if (distanceToPlayer <= attackRange)
        {
            float attackBase = CalculateAttackUtility(distanceToPlayer, healthPercent);
            utilities[CharacterState.Attack] = attackBase * previewCharacter.attackTendency;
        }

        utilities[CharacterState.Move] = CalculateMoveTowardUtility(distanceToPlayer, healthPercent);
        utilities[CharacterState.MoveBack] = CalculateMoveAwayUtility(distanceToPlayer, healthPercent) * previewCharacter.retreatTendency;

        float blockUtility = CalculateBlockUtility(distanceToPlayer, healthPercent);

        if (playerState == CharacterState.Attack || playerState == CharacterState.Attack1 || playerState == CharacterState.Attack2)
        {
            blockUtility *= 3f;
        }

        if (distanceToPlayer > attackRange && healthPercent < previewCharacter.healThreshold)
        {
            utilities[CharacterState.Heal] = CalculateHealUtility(healthPercent) * previewCharacter.healTendency;
        }
        else
        {
            utilities[CharacterState.Block] = blockUtility * previewCharacter.blockTendency;
        }

        utilities[CharacterState.Idle] = 0.2f;

        // 选择最优行动
        CharacterState bestState = CharacterState.Idle;
        float maxUtility = float.MinValue;
        foreach (var kvp in utilities)
        {
            if (kvp.Value > maxUtility)
            {
                maxUtility = kvp.Value;
                bestState = kvp.Key;
            }
        }

        if (Random.value < (1f - previewCharacter.aggressiveness))
        {
            float r = Random.value;
            if (r < 0.3f) bestState = CharacterState.Move;
            else if (r < 0.6f) bestState = CharacterState.Idle;
        }

        // 如果决定攻击，再根据倾向值随机选择具体攻击方式
        if (bestState == CharacterState.Attack)
        {
            bestState = ChooseAttackType();
            StopAIBlock();
            ChangeState(bestState);
        }
        else if (bestState == CharacterState.Block)
        {
            StartAIBlock(0.5f);
        }
        else if (bestState == CharacterState.Heal)
        {
            StartHealing();
        }
        else
        {
            StopAIBlock();
            ChangeState(bestState);
        }
    }

    // 根据倾向值加权随机选择攻击类型
    private CharacterState ChooseAttackType()
    {
        float total = previewCharacter.attack0Tendency +
                      previewCharacter.attack1Tendency +
                      previewCharacter.attack2Tendency;

        float rand = Random.value * total;

        if (rand < previewCharacter.attack0Tendency)
        {
            return CharacterState.Attack;
        }
        else if (rand < previewCharacter.attack0Tendency + previewCharacter.attack1Tendency)
        {
            return CharacterState.Attack1;
        }
        else
        {
            return CharacterState.Attack2;
        }
    }

    private void StartAIBlock(float duration)
    {
        aiHoldingBlock = true;
        blockHoldEndTime = Time.time + duration;

        if (currentState != CharacterState.Block)
        {
            ChangeState(CharacterState.Block);
        }
    }

    private void StopAIBlock()
    {
        if (!aiHoldingBlock) return;

        aiHoldingBlock = false;

        if (currentState == CharacterState.Block)
        {
            ChangeState(CharacterState.Idle);
        }
    }

    private float CalculateAttackUtility(float distance, float healthPercent)
    {
        if (distance > attackRange) return 0f;

        float utility = 1.0f;
        utility *= Mathf.Lerp(0.5f, 1.0f, healthPercent);
        utility *= Mathf.Clamp01((attackRange - distance * 0.7f) / attackRange);

        if (distance < attackRange * 0.7f)
        {
            utility *= 1.3f;
        }

        return utility * previewCharacter.aggressiveness;
    }

    private float CalculateBlockUtility(float distance, float healthPercent)
    {
        if (distance > attackRange * 1.2f) return 0f;

        float utility = 0.5f;
        utility *= (1f - healthPercent * 0.4f);

        if (playerControl != null &&
            (playerControl.GetCurrentState() == CharacterState.Attack ||
             playerControl.GetCurrentState() == CharacterState.Attack1 ||
             playerControl.GetCurrentState() == CharacterState.Attack2))
        {
            utility *= 3f;
        }

        if (distance < attackRange * 0.8f)
        {
            utility *= 1.5f;
        }

        return utility * (previewCharacter.aggressiveness < 0.5f ? 2f : 1f);
    }

    private float CalculateHealUtility(float healthPercent)
    {
        float utility = (1f - healthPercent) * 2f;
        return utility * (previewCharacter.aggressiveness < 0.3f ? 2f : 1f);
    }

    private float CalculateMoveTowardUtility(float distance, float healthPercent)
    {
        if (distance < attackRange * 1.2f) return 0f;

        float utility = 0.7f;
        utility *= healthPercent;
        utility *= Mathf.Clamp01(distance / safeDistance);

        return utility;
    }

    private float CalculateMoveAwayUtility(float distance, float healthPercent)
    {
        float utility = 0.6f;
        utility *= (1f - healthPercent);
        utility *= Mathf.Clamp01(1f - distance / safeDistance);

        if (distance < attackRange * 0.8f)
        {
            utility += 0.3f;
        }

        utility *= Mathf.Clamp01((attackRange - distance) / attackRange);

        return utility;
    }

    private void ExecuteCurrentState()
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
                StopMovement();
                break;
        }
    }

    private void MoveTowardPlayer()
    {
        if (stateLocked) return;
        float direction = Mathf.Sign(playerTransform.position.x - transform.position.x);
        Move(direction);
    }

    private void MoveAwayFromPlayer()
    {
        if (stateLocked) return;
        float direction = -Mathf.Sign(playerTransform.position.x - transform.position.x);
        Move(direction);
    }

    protected override void OnStateComplete()
    {
        base.OnStateComplete();
        needsNewDecision = true;
    }

    protected override void OnTakeDamage()
    {
        base.OnTakeDamage();
        needsNewDecision = true;
    }

    protected override void OnOpponentAction()
    {
        base.OnOpponentAction();
    }

    protected override void OnCharacterDeath()
    {
        base.OnCharacterDeath();

        VolumeManager.Instance.swapMusic(VolumeManager.Instance.bgmDefeat, false);

        var player = BattleManager.Instance.GetPlayerCharacterControl();
        if (player != null)
        {
            player.ShowVictory();
        }
    }
}