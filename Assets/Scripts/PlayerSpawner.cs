using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class NetworkPlayerSpawner : MonoBehaviour
{
    public static NetworkPlayerSpawner Instance;

    [Header("Spawn Range")]
    public float range = 10f;

    private readonly List<Vector2> usedPositions =
        new List<Vector2>();

    private void Awake()
    {
        Instance = this;
    }

    public Vector2 GetSpawnPosition()
    {
        for (int i = 0; i < 100; i++)
        {
            Vector2 pos =
                new Vector2(
                    Random.Range(-range, range),
                    Random.Range(-range, range)
                );

            bool overlap = false;

            foreach (var used in usedPositions)
            {
                if (
                    Vector2.Distance(
                        pos,
                        used
                    ) < 5f
                )
                {
                    overlap = true;
                    break;
                }
            }

            if (!overlap)
            {
                usedPositions.Add(pos);
                return pos;
            }
        }

        return Vector2.zero;
    }
}