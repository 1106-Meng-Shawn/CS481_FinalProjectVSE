using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreviewCharacter : MonoBehaviour
{
    public string Name;
    public Sprite Annoying;
    public Sprite Groggy;
    public Sprite Hit;
    public Sprite Win;
    public Sprite Lose;
    public Sprite Attack0;
    public Sprite Attack1;
    public Sprite Attack2;
    public Sprite Portrait;
    public Sprite PortraitLose;
    public Sprite Stand;

    [Header("Movement")]
    [SerializeField] public float moveSpeed = 5f;

    [Header("Combat")]
    [SerializeField] public float attackDuration = 0.75f;
    [SerializeField] public float attack1Duration = 0.5f;
    [SerializeField] public float attack2Duration = 0.25f;
    [SerializeField] public float blockDuration = 1f;
    [SerializeField] public float HealDuration = 2f;
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float AttackDamage = 20f;
    [SerializeField] public float Attack1Damage = 10f;
    [SerializeField] public float Attack2Damage = 5f;
    [SerializeField] public float hurtDuration = 0.125f;
    [SerializeField] public float HealAmount = 1f;
    [SerializeField][Range(0f, 10f)] public int jumpForce = 10;

    [SerializeField][Range(0f, 1f)] public float blockReduction = 0.3f;


    [Header("Audio")]
    [SerializeField] public AudioSource audioSource;
    [SerializeField] public AudioClip attackSFX;
    [SerializeField] public AudioClip attack1SFX;
    [SerializeField] public AudioClip attack2SFX;
    [SerializeField] public AudioClip blockSFX;
    [SerializeField] public AudioClip victorySFX;
    [SerializeField] public AudioClip defeatSFX;
    [SerializeField] public AudioClip jumpSFX;

    [Header("AI Personality")]
    [SerializeField][Range(0f, 1f)] public float aggressiveness = 0.5f;
    [SerializeField][Range(0f, 1f)] public float healThreshold = 0.3f;
    [SerializeField][Range(0f, 1f)] public float jumpAttackChance = 0.3f;

    [Header("AI Action Tendencies")]
    [SerializeField][Range(0f, 10f)] public float attack0Tendency = 10f;
    [SerializeField][Range(0f, 10f)] public float attack1Tendency = 10f;
    [SerializeField][Range(0f, 10f)] public float attack2Tendency = 10f;
    [SerializeField][Range(0f, 10f)] public float blockTendency = 1f;
    [SerializeField][Range(0f, 10f)] public float healTendency = 1f;
    [SerializeField][Range(0f, 10f)] public float attackTendency = 10f;
    [SerializeField][Range(0f, 10f)] public float retreatTendency = 1f;

    void Start()
    {
    }

    void Update()
    {
    }
}