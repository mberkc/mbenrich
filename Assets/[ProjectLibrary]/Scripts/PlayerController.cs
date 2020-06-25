using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Turn {
    None = 0,
    Left = 1,
    Right = 2
}

public class PlayerController : MonoBehaviour, IPlayerController, IAIController {

    #region Variables
    Rigidbody rb;
    Animator animator;
    CapsuleCollider characterCollider;
    Vector3 lastCheckpointPos, positionToTurn;
    Quaternion lastCheckpointRot;
    Collider[] rigColliders;
    Rigidbody[] rigRigidbodies;
    float speed = 10f, turnControlErrorConstant = 0.2f;
    Turn rotate = Turn.None;

    public int nextSpawnPos;
    bool canTouch = false;
    #endregion

    #region Properties
    public bool CanTouch {
        get { return canTouch; }
        set {
            if (GameManager.Instance.isAIEnabled)
                canTouch = false;
            else
                canTouch = value;
        }
    }
    #endregion

    void Awake () {
        rb = GetComponent<Rigidbody> ();
        animator = GetComponent<Animator> ();
        characterCollider = GetComponent<CapsuleCollider> ();
        rigColliders = GetComponentsInChildren<Collider> ();
        rigRigidbodies = GetComponentsInChildren<Rigidbody> ();
    }

    void Start () {
        CheckPoint ();
    }

    void Update () {
        if (canTouch)
            TouchController ();
        else if (GameManager.Instance.isAIEnabled)
            AIMovementControl ();
        if (transform.position.y < -5) {
            canTouch = false;
            GameManager.Instance.LoseHealth ();
        }
    }

    void FixedUpdate () {
        if (rotate != 0)
            TurnControl ();
    }

    void TouchController () {
        if (Input.GetMouseButton (0))
            Move ();
        else if (Input.GetMouseButtonUp (0))
            Stop ();
    }

    public void Move () {
        animator.SetBool ("isMoving", true);
        animator.SetFloat ("speed", speed);
        // SpawnControl
        if (transform.position.z >= nextSpawnPos) {
            // If there is End don't spawn
            if (nextSpawnPos == -1)
                return;
            nextSpawnPos = GameManager.Instance.SpawnPlatforms ();
        }
    }

    public void Stop () {
        animator.SetBool ("isMoving", false);
    }

    public void TurnControl () {
        if (Vector2.Distance (new Vector2 (transform.position.x, transform.position.z), new Vector2 (positionToTurn.x, positionToTurn.z)) < turnControlErrorConstant) {
            if (rotate == Turn.Left)
                TurnLeft ();
            else if (rotate == Turn.Right)
                TurnRight ();
        }
    }

    public void TurnLeft () {
        transform.rotation = Quaternion.Euler (0f, transform.rotation.eulerAngles.y - 90f, 0f);
        rotate = 0;
    }

    public void TurnRight () {
        transform.rotation = Quaternion.Euler (0f, transform.rotation.eulerAngles.y + 90f, 0f);
        rotate = 0;
    }

    public void AIMovementControl () {
        // AI Movement Control - Raycast
    }

    void CheckPoint () {
        lastCheckpointPos = transform.position;
        lastCheckpointRot = transform.rotation;
    }

    void CenterPlayer () {
        // Before Left/Right Turns
        //transform.position = transform.TransformDirection (new Vector3 (0, transform.position.y, transform.position.z));
    }

    public void Respawn () {
        EnableRagdoll (false, false);
        gameObject.SetActive (false);
        transform.position = lastCheckpointPos;
        transform.rotation = lastCheckpointRot;
        gameObject.SetActive (true);
        canTouch = true;
    }

    public void EnableRagdoll (bool active, bool isGameOver) {
        if (active) {
            animator.enabled = false;

            foreach (Collider col in rigColliders)
                col.enabled = true;
            foreach (Rigidbody rBody in rigRigidbodies)
                if (rb != rBody)
                    rBody.isKinematic = false;

            characterCollider.enabled = false;
            rb.useGravity = false;
            if (!isGameOver)
                Invoke ("Respawn", 3f);
        } else {

            foreach (Collider col in rigColliders)
                col.enabled = false;
            foreach (Rigidbody rBody in rigRigidbodies)
                if (rb != rBody)
                    rBody.isKinematic = true;

            animator.enabled = true;
            characterCollider.enabled = true;
            rb.useGravity = true;
        }
    }

    void OnCollisionEnter (Collision other) {
        if (other.transform.CompareTag (Tags.OBSTACLE)) {
            canTouch = false;
            GameManager.Instance.LoseHealth ();
        } else if (other.transform.CompareTag (Tags.CHECKPOINT)) {
            CheckPoint ();
        } else if (other.transform.CompareTag (Tags.TURN_LEFT)) {
            positionToTurn = other.transform.position;
            rotate = Turn.Left;
        } else if (other.transform.CompareTag (Tags.TURN_RIGHT)) {
            positionToTurn = other.transform.position;
            rotate = Turn.Right;
        } else if (other.transform.CompareTag (Tags.FINISH)) {
            canTouch = false;
            if (animator.GetBool ("isMoving"))
                Stop ();
            GameManager.Instance.Win ();
        }
    }

    void OnCollisionExit (Collision other) {
        if (other.transform.CompareTag (Tags.MOVING_PLATFORM) || other.transform.CompareTag (Tags.TURN_LEFT) || other.transform.CompareTag (Tags.TURN_RIGHT))
            CenterPlayer ();
    }

}