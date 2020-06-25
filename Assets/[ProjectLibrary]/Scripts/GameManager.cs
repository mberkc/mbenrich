using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState {
    None,
    Starting,
    Playing,
    Respawning,
    Ending,
}

public enum TurnState {
    None = 0,
    Left = 1,
    Right = 2
}

public class GameManager : MonoBehaviour {
    #region Variables
    public static GameManager Instance;

    [Space (10)]
    [Header ("MUST SET")]
    public int maxDifficulty = 5;

    [Space (10)]
    [Header ("GAMEPLAY ELEMENTS")]

    public GameObject endPlatform;
    GameObject player;

    [Space (10)]
    [Header ("VARIABLES")]
    public bool isAIEnabled = false;
    bool isLoadingScene;

    [SerializeField]
    GameState currentGameState = GameState.None;

    PlayerController playerController;
    ObjectPooler objectPooler;
    TurnState turnState;
    int difficulty = 1, health, level;
    int nextZPosToSpawn, nextXPosToSpawn, spawnDiff, minZPosToFinish = 140;
    #endregion

    #region Properties
    public int Difficulty {
        get { return difficulty; }
        set { difficulty = value; }
    }
    #endregion

    #region Initializing

    void Awake () {
        if (Instance == null) {
            Instance = this;
            level = 1;
            DontDestroyOnLoad (gameObject);
        } else if (Instance != this)
            Destroy (gameObject);

    }

    void Start () {
        objectPooler = GetComponent<ObjectPooler> ();
        SetGame ();
    }

    #endregion

    #region Condition Handling & Other Functions

    void SetGame () {
        if (GetGameState () != GameState.Starting) {
            SetGameState (GameState.Starting);
            player = GameObject.FindGameObjectWithTag (Tags.PLAYER);
            playerController = player.GetComponent<PlayerController> ();
            health = 3;
            nextZPosToSpawn = 10;
            nextXPosToSpawn = 0;
            spawnDiff = 70;
            playerController.nextSpawnPos = SpawnPlatforms ();
            ReadyToPlay ();
        }
    }

    public int SpawnPlatforms () {
        int length = 0;
        for (int i = 0; i < 10; i++) {
            float random = UnityEngine.Random.Range (0f, 100f);
            string name = "";
            if (random <= 5f) {
                length = 4;
                if (nextZPosToSpawn > minZPosToFinish) {
                    Instantiate (endPlatform, transform.position + Vector3.forward * (nextZPosToSpawn + length / 2), Quaternion.identity);
                    return -1;
                } else
                    name = Constants.SHORT;
            } else if (random <= 15f) {
                name = Constants.LONG;
                length = 10;
            } else if (random <= 32f) {
                name = Constants.MOVING;
                length = 4;
            } else if (random <= 49f) {
                name = Constants.ROTATING;
                length = 10;
            } else if (random <= 66f) {
                name = Constants.UPDOWN;
                length = 10;
            } else if (random <= 83f) {
                name = Constants.SQUEEZING;
                length = 10;
            } else if (random <= 100f) { // TurnLeft/Right
                name = Constants.SHORT;
                length = 4;
                if (turnState == TurnState.None) {
                    if (random <= 91.5f)
                        turnState = TurnState.Left;
                    else
                        turnState = TurnState.Right;
                }
            }
            GameObject go = objectPooler.SpawnFromPool (name, transform.position + Vector3.forward * (nextZPosToSpawn + length / 2) + Vector3.right * nextXPosToSpawn, Quaternion.identity);
            if (turnState == TurnState.Left) {
                go.tag = Tags.TURN_LEFT;
                nextXPosToSpawn -= 7;
                GameObject goLong = objectPooler.SpawnFromPool (Constants.LONG, new Vector3 (nextXPosToSpawn, go.transform.position.y, go.transform.position.z), Quaternion.identity);
                goLong.transform.rotation = Quaternion.Euler (0f, -90f, 0f);
                nextXPosToSpawn -= 7;
                GameObject goRight = objectPooler.SpawnFromPool (name, new Vector3 (nextXPosToSpawn, go.transform.position.y, go.transform.position.z), Quaternion.identity);
                goRight.tag = Tags.TURN_RIGHT;
                turnState = TurnState.None;
            } else if (turnState == TurnState.Right) {
                go.tag = Tags.TURN_RIGHT;
                nextXPosToSpawn += 7;
                GameObject goLong = objectPooler.SpawnFromPool (Constants.LONG, new Vector3 (nextXPosToSpawn, go.transform.position.y, go.transform.position.z), Quaternion.identity);
                goLong.transform.rotation = Quaternion.Euler (0f, 90f, 0f);
                nextXPosToSpawn += 7;
                GameObject goLeft = objectPooler.SpawnFromPool (name, new Vector3 (nextXPosToSpawn, go.transform.position.y, go.transform.position.z), Quaternion.identity);
                goLeft.tag = Tags.TURN_LEFT;
                turnState = TurnState.None;
            }
            if (go.CompareTag (Tags.CHECKPOINT))
                nextZPosToSpawn += 10;
            else
                nextZPosToSpawn += 4;
        }
        return nextZPosToSpawn - spawnDiff;
    }

    void ReadyToPlay () {
        if (GetGameState () == GameState.Starting) {
            playerController.CanTouch = true;
            SetGameState (GameState.Playing);
        }
    }
    public void LoseHealth () {
        if (GetGameState () == GameState.Playing) {
            SetGameState (GameState.Respawning);
            if (--health <= 0) {
                playerController.EnableRagdoll (true, true);
                GameOver ();
                return;
            }
            playerController.EnableRagdoll (true, false);
            SetGameState (GameState.Playing);
        }
    }

    public void Win () {
        if (GetGameState () != GameState.Ending) {
            SetGameState (GameState.Ending);
            AdvanceLevel ();
        }
    }

    public void GameOver () {
        if (GetGameState () != GameState.Ending) {
            SetGameState (GameState.Ending);
            RestartLevel ();
        }
    }
    #endregion

    #region GameState Handling

    public GameState GetGameState () {
        return currentGameState;
    }

    public void SetGameState (GameState gameState) {
        currentGameState = gameState;
    }

    #endregion

    #region Level Management
    void AdvanceLevel () {
        if (GetGameState () == GameState.Ending) {
            SetGameState (GameState.None);
            minZPosToFinish += ++level * 20;
            if (difficulty < maxDifficulty)
                objectPooler.SetDifficulty (++difficulty);
            StartCoroutine (LoadScene ());
        }
    }

    void RestartLevel () {
        if (GetGameState () == GameState.Ending) {
            SetGameState (GameState.None);
            StartCoroutine (LoadScene ());
        }
    }

    public IEnumerator LoadScene () {
        if (!isLoadingScene) {
            isLoadingScene = true;
            CancelInvoke ();
            objectPooler.Reset ();
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync (0);
            while (!asyncLoad.isDone) {
                yield return null;
            }
            isLoadingScene = false;
            SetGame ();
        }
    }

    #endregion
}