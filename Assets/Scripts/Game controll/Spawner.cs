using Mirror;
using System;
using UnityEngine;


public class Spawner : NetworkBehaviour {

    [Tooltip("Keep 0 for endless spawn")]
    [SerializeField] private int maxSpawnCount;
    [SerializeField] private float delay = 1;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private EnemyScript[] enemies;
    private int spawnCount = 0;
    private float nextSpawnTime;

    private void Update() {

        if (!isServer) return; 

        if (ShouldSpawn()) {
            Spawn();
        }
    }

    private void Spawn() {
        nextSpawnTime = Time.time + delay;
        Transform spawnPoint = ChooseSpawnPoint();
        EnemyScript enemyPrefab = ChooseEnemy();
        EnemyScript enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkServer.Spawn(enemy.gameObject, connectionToClient);
        spawnCount++;
    }

    private EnemyScript ChooseEnemy() {
        int randomIndex = UnityEngine.Random.Range(0, enemies.Length);
        EnemyScript enemy = enemies[randomIndex];
        return enemy;
    }

    private Transform ChooseSpawnPoint() {
        int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];
        return spawnPoint;
    }

    private bool ShouldSpawn() {

        if (maxSpawnCount == 0)
            return Time.time >= nextSpawnTime;

        return spawnCount < maxSpawnCount ? Time.time >= nextSpawnTime : false;
    }
}

