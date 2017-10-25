﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibHighLevel;

public class CustomNetworkManager : LiteNetLibGameManager {
    public static CustomNetworkManager Singleton { get; private set; }
    protected override void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Singleton = this;
        base.Awake();
    }
}
