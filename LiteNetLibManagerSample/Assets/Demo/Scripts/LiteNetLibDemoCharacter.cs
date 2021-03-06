﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LiteNetLib;
using LiteNetLibManager;

public class LiteNetLibDemoCharacter : LiteNetLibBehaviour
{
    public enum TestEnum
    {
        EnumA = int.MaxValue,
        EnumB = int.MinValue
    }

    public SyncFieldInt hp;
    public SyncFieldString testString = new SyncFieldString()
    {
        syncMode = LiteNetLibSyncField.SyncMode.ClientMulticast,
        deliveryMethod = DeliveryMethod.ReliableOrdered,
    };
    [SyncField(syncMode = LiteNetLibSyncField.SyncMode.ServerToClients, hook = "TestHook")]
    public int testSyncField;
    [SyncField(syncMode = LiteNetLibSyncField.SyncMode.ServerToClients, hook = "TestHook2")]
    public TestEnum testSyncField2 = TestEnum.EnumA;
    [SyncField(syncMode = LiteNetLibSyncField.SyncMode.ServerToClients, hook = "TestHook3")]
    public PackedLong testSyncField3 = new PackedLong(long.MaxValue);
    public long testSyncField3_Value = long.MaxValue;
    public int bulletType;
    public int maxHp = 100;
    public float rotateSpeed = 150f;
    public float moveSpeed = 5f;
    public LiteNetLibDemoBullet[] bullets;
    public Text testStringText;

    private void Awake()
    {
        bulletType = 0;
    }

    private void Start()
    {
        if (IsServer)
            hp.Value = maxHp;
    }

    private void LateUpdate()
    {
        if (!IsServer)
            return;

        if (testSyncField3 != testSyncField3_Value)
            testSyncField3 = testSyncField3_Value;
    }

    private void TestHook(int value)
    {
        Debug.LogError("[TestHook] " + value);
    }

    private void TestHook2(TestEnum value)
    {
        Debug.LogError("[TestHook2] " + value);
    }

    private void TestHook3(PackedLong value)
    {
        Debug.LogError("[TestHook3] " + (long)value);
    }

    public override void OnSetOwnerClient(bool isOwnerClient)
    {
        base.OnSetOwnerClient(isOwnerClient);
        if (isOwnerClient)
        {
            LiteNetLibDemoUIGameplay.Singleton.owningCharacter = this;
            var followCam = FindObjectOfType<FollowCameraControls>();
            followCam.target = transform;
        }
    }

    public override void OnSetup()
    {
        base.OnSetup();
        hp.onChange = (init, value) =>
        {
            Debug.LogError("[Hp change] " + init + " " + value);
        };
        testString.onChange = (init, value) =>
        {
            Debug.LogError("[TestString change] " + init + " " + value);
        };
        RegisterNetFunction("RespawnAtPoint", new LiteNetLibFunction<Vector3>((position) => RespawnAtPoint(position)));
    }

    [NetFunction]
    protected virtual void Shoot(int bulletType)
    {
        SpawnBullet(bulletType);
    }

    private void Update()
    {
        if (IsOwnerClient)
        {
            var x = Input.GetAxis("Horizontal") * Time.deltaTime * rotateSpeed;
            var z = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

            transform.Rotate(0, x, 0);
            transform.Translate(0, 0, z);

            if (Input.GetKeyDown(KeyCode.Alpha1))
                bulletType = 0;

            if (Input.GetKeyDown(KeyCode.Alpha2))
                bulletType = 1;

            if (Input.GetKeyDown(KeyCode.Alpha3))
                bulletType = 2;

            if (Input.GetKeyDown(KeyCode.Space))
                Shoot();

            LiteNetLibDemoUIGameplay.Singleton.SetActiveBulletType(bulletType);
            LiteNetLibDemoUIGameplay.Singleton.SetHp(hp.Value);
        }

        testStringText.text = testString.Value;

        if (IsServer && hp.Value <= 0)
            Respawn();
    }

    public void SpawnBullet(int bulletType)
    {
        if (!IsServer || bulletType < 0 || bulletType >= bullets.Length)
            return;
        var bullet = Instantiate(bullets[bulletType], transform.position, transform.rotation);
        var bulletIdentity = Manager.Assets.NetworkSpawn(bullet.gameObject);
        var bulletComp = bulletIdentity.GetComponent<LiteNetLibDemoBullet>();
        bulletComp.attacker = this;
    }

    public void RespawnAtPoint(Vector3 position)
    {
        transform.position = position;
    }

    public void Shoot()
    {
        CallNetFunction("Shoot", DeliveryMethod.ReliableOrdered, FunctionReceivers.Server, bulletType);
    }

    void Respawn()
    {
        hp.Value = maxHp;
        CallNetFunction("RespawnAtPoint", ConnectionId, Manager.Assets.GetPlayerSpawnPosition());
    }
}
