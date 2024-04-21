using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnersManager : MonoBehaviour
{
    [Tooltip("Spawners available to Red players.")]
    public List<Transform> RedSpawners = new();
    [Tooltip("Spawners available to Blue players.")]
    public List<Transform> BlueSpawners = new();
    [Tooltip("The distance players may spawn from spawner.")]
    public float Range = 10.0f;


    public void SpawnActor(Actor actor)
    {
        NavMeshHit nearestHit = ReadySpawner(actor);
        Instantiate(actor, nearestHit.position, Quaternion.identity);
    }

    public void RespawnActor(Actor actor)
    {
        NavMeshHit nearestHit = ReadySpawner(actor);
        actor.gameObject.transform.position = nearestHit.position;
        actor.gameObject.SetActive(true);
    }

    public NavMeshHit ReadySpawner(Actor actor)
    {
        int team = actor.GetAffiliation();
        Transform spawner = GetRandomTeamSpawner(team);
        NavMeshHit nearestHit;
        NavMesh.SamplePosition(spawner.position, out nearestHit, 10, 1);
        return nearestHit;
    }

    Transform GetRandomTeamSpawner(int affiliation)
    {
        List<Transform> SpawnerList;

        if (affiliation == 0)
        {
            SpawnerList = RedSpawners;
        }
        else
        {
            SpawnerList = BlueSpawners;
        }
        int spawnIndex = Random.Range(0, SpawnerList.Count);
        return SpawnerList[spawnIndex];
    }
}
