using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorsManager : MonoBehaviour
{
    [Tooltip("Players in the current game.")]
    public List<Actor> Actors;
    public GameObject Player { get; private set; }

    SpawnersManager _spawnersManager;

    public void SetPlayer(GameObject player) => Player = player;

    void Awake()
    {
        _spawnersManager = FindObjectOfType<SpawnersManager>();
        DebugUtility.HandleErrorIfNullFindObject<SpawnersManager, ActorsManager>(_spawnersManager, this);

        foreach (Actor actor in Actors)
        {
            actor.gameObject.SetActive(true);
            _spawnersManager.SpawnActor(actor);
        }
    }

    public void RespawnActor(Actor actor)
    {
        StartCoroutine(DelayRespawn(actor));
    }

    IEnumerator DelayRespawn(Actor actor)
    {
        actor.gameObject.SetActive(false);
        yield return new WaitForSeconds(5f);
        _spawnersManager.RespawnActor(actor);
    }
}
