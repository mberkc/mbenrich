using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Pool {
    public string name;
    public GameObject prefab;
    public int size;
}

public class ObjectPooler : MonoBehaviour {

    #region Variables

    ObjectPooler Instance;
    public List<Pool> pools;

    public static Dictionary<string, Queue<GameObject>> poolDictionary;
    public List<Animation> animatedPlatforms;
    #endregion

    void Awake () {
        poolDictionary = new Dictionary<string, Queue<GameObject>> ();
        foreach (Pool pool in pools) {
            Queue<GameObject> objectPool = new Queue<GameObject> ();
            for (int i = 0; i < pool.size; i++) {
                GameObject obj = Instantiate (pool.prefab);
                obj.SetActive (false);
                objectPool.Enqueue (obj);
                switch (pool.name) {
                    case Constants.ROTATING:
                    case Constants.SQUEEZING:
                    case Constants.UPDOWN:
                        SetAnimatedPlatforms (obj);
                        break;
                }
            }
            poolDictionary.Add (pool.name, objectPool);
        }
    }

    public void Reset () {
        poolDictionary.Clear ();
    }

    void SetAnimatedPlatforms (GameObject go) {
        foreach (Animation anim in go.GetComponentsInChildren<Animation> ())
            animatedPlatforms.Add (anim);
    }

    public GameObject SpawnFromPool (string key, Vector3 position, Quaternion rotation) {
        if (!poolDictionary.ContainsKey (key))
            return null;
        GameObject obj = poolDictionary[key].Dequeue ();
        if (obj.CompareTag (Tags.TURN_LEFT) || obj.CompareTag (Tags.TURN_RIGHT))
            obj.tag = "Untagged";
        obj.SetActive (true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        poolDictionary[key].Enqueue (obj);
        return obj;
    }

    public void SetDifficulty (int difficulty) {
        foreach (Animation animation in animatedPlatforms)
            foreach (AnimationState state in animation)
                state.speed = 1 - (((difficulty - 1) / GameManager.Instance.maxDifficulty) / 2);
    }
}