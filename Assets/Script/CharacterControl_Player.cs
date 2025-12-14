using UnityEngine;

public static class PlayerControl
{
    public static KeyCode playerLeftMove = KeyCode.A;
    public static KeyCode playerRightMove = KeyCode.D;
    public static KeyCode playerAttack0 = KeyCode.J;
    public static KeyCode playerAttack1 = KeyCode.N;
    public static KeyCode playerAttack2 = KeyCode.U;
    public static KeyCode playerBlock = KeyCode.K;
    public static KeyCode playerHeal = KeyCode.S;
    public static KeyCode playerJump = KeyCode.Space;
}

public class CharacterControl_Player : CharacterControl
{
    protected override void HandleControl()
    {

        HandleAttackInput();   
        HandleHealInput();
        if (currentState != CharacterState.Attack && currentState != CharacterState.Block)
        {
            HandleMoveInput();
        }
        if (Input.GetKeyDown(PlayerControl.playerJump))
        {
            PerformJump();
        }
        HandleBlockInput();

    }

    void HandleMoveInput()
    {
        float h = 0;
        if (Input.GetKey(PlayerControl.playerLeftMove)) h = -1;
        if (Input.GetKey(PlayerControl.playerRightMove)) h = 1;

        if (h != 0)
        {
            Move(h);
            ChangeState(CharacterState.Move);
        }
        else
        {
            StopMovement();
            ChangeState(CharacterState.Idle);
        }
    }

    void HandleAttackInput()
    {
        if (Input.GetKeyDown(PlayerControl.playerAttack0))
        {
            PerformAttack();
            NotifyOpponentOfAction();
        }
        if (Input.GetKeyDown(PlayerControl.playerAttack1))
        {
            PerformAttack1();
            NotifyOpponentOfAction();
        }
        if (Input.GetKeyDown(PlayerControl.playerAttack2))
        {
            PerformAttack2();
            NotifyOpponentOfAction();
        }
    }

    void HandleBlockInput()
    {
        if (Input.GetKey(PlayerControl.playerBlock))
        {
            if (currentState != CharacterState.Block)
            {
                PerformBlock();
            }
        }
        else
        {
            if (currentState == CharacterState.Block)
            {
                ChangeState(CharacterState.Idle);
            }
        }
    }



    void HandleHealInput()
    {
        if (Input.GetKey(PlayerControl.playerHeal))
        {
            if (!isHealing)
            {
                //VolumeManager.Instance.playHealing();
                StartHealing();
            }
        }
        else
        {
            if (isHealing)
            {
                StopHealing();
            }
        }
    }

    protected override void OnCharacterDeath()
    {
        base.OnCharacterDeath();

        VolumeManager.Instance.swapMusic(VolumeManager.Instance.bgmDefeat, false);

        var enemy = BattleManager.Instance.GetEnemyCharacterControl();
        if (enemy != null)
        {
            enemy.ShowVictory();
        }
    }
}