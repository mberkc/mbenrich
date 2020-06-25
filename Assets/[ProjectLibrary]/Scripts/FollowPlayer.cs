using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour {
    Transform playerTransform;
    float offsetZ;
    void Awake () {
        offsetZ = transform.position.z;
        playerTransform = GameObject.FindGameObjectWithTag (Tags.PLAYER).transform;
    }

    void LateUpdate () {
        transform.position = new Vector3 (playerTransform.position.x, transform.position.y, playerTransform.position.z + offsetZ);
    }
}