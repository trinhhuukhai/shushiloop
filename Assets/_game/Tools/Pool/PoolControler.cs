using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PoolControler : SingletonMonoBehaviour<PoolControler>
{


    [Header("Other Pools")]
    public PoolAmount[] otherPools;

    [Header("Particle")]
    public ParticleAmount[] Particle;

    private IEnumerable<PoolAmount> AllPools()
    {
     
        if (otherPools != null) foreach (var p in otherPools) yield return p;

    }

    public override void Awake()
    {
        base.Awake();

        // Particle
        foreach (var pa in Particle)
        {
            ParticlePool.Preload(pa.prefab, pa.amount, pa.root);
        }

        // Tất cả pool GameUnit
        foreach (var pool in AllPools())
        {
            if (pool.prefab == null) continue;
            SimplePool.Preload(pool.prefab, pool.amount,
                               pool.root, pool.collect, pool.clamp);
        }
    }
}

