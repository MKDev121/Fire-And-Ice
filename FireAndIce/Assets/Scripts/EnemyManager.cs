using System.Collections.Generic;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyManager : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef[] _enemiesPrefab;
    private List<NetworkObject> currentEnemies;

    [Networked] private TickTimer timer { get; set; }
    [Networked] private TickTimer stateTimer { get; set; }

    public float enemySpawnTime = 3f;
    public float changeStateTime = 5f;
    public void StartTimer(NetworkRunner runner)
    {
        if (runner != null && runner.IsServer)
        {
            timer = TickTimer.CreateFromSeconds(runner, 1);
            stateTimer = TickTimer.CreateFromSeconds(runner, changeStateTime);
            Debug.Log("Hello");
            currentEnemies = new List<NetworkObject>();
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (timer.Expired(Runner))
        {
            if (currentEnemies.Count < 5)
            {
                Debug.Log("Spawn Enemy");
                int idx = Random.Range(0, 2);
                float ypos = 0f;
                switch (idx)
                {
                    case 0:
                        ypos = -2.58f;
                        break;
                    case 1:
                        ypos = Random.Range((int)1, 5) * 2f;

                        break;
                }
                Vector3 spawnPos = new Vector3(Random.Range(0, 12) % 12, ypos, -.2f);
                NetworkObject enemy = Runner.Spawn(_enemiesPrefab[idx], position: spawnPos);
                enemy.transform.position = spawnPos;

                currentEnemies.Add(enemy);
            }
            timer = TickTimer.CreateFromSeconds(Runner, enemySpawnTime);
        }

        if (stateTimer.Expired(Runner))
        {
            if (currentEnemies.Count > 0)
            {
                foreach (NetworkObject enemy in currentEnemies)
                {
                    Enemy script = enemy.GetComponent<Enemy>();
                    script.changeState = true;
                }
            }
            stateTimer = TickTimer.CreateFromSeconds(Runner, changeStateTime);
        }
    }
    public void removeEnemy(NetworkObject obj)
    {

        currentEnemies.Remove(obj);
    }
    public void gameOver()
    {
        foreach (NetworkObject enemy in currentEnemies)
        {
            enemy.GetComponent<Enemy>().enabled = false;
        }
        this.enabled = false;
    }
}

