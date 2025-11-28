using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BackgroundType
{
    Grassland, Snowfield, Desert
}


public class BackgroundManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private BackgroundType currentBackgroundType;

    public static BackgroundManager Instance { get; private set; }


    [SerializeField] public List<Background> Backgrounds;


    [Serializable]
    public struct Background
    {
        public BackgroundType BackgroundType;
        public GameObject BackgroundOj;

    }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }


    public void SetBackground(BackgroundType backgroundType)
    {
        currentBackgroundType = backgroundType;

        foreach (var bg in Backgrounds)
        {
            if (bg.BackgroundOj == null) continue;

            bool isActive = bg.BackgroundType == backgroundType;
            bg.BackgroundOj.SetActive(isActive);
        }
    }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
