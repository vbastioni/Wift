﻿using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour {

    [SerializeField]
    private Text[] playerScores;
    private int[] playerCachedScore;
    private int[] playerScoreBonusMultiplier;
    [SerializeField]
    private Text[] playerDeath;
    private int[] playerCachedDeath;
    private int[] playerPowerUpJauge;

    [SerializeField]
    private Text[] playerMultText;

    [SerializeField]
    private int powerUpPerPoint = 10;

    [SerializeField]
    private float multLostPercentOnDeath = 0.1f;

    [SerializeField]
    private bool debug = false;
    private PoolManager poolManager;

    [SerializeField]
    private Poolable multiplierPrefab;
    private Pool multiplierPool;

    [SerializeField]
    private Image[] gauges;

    [SerializeField]
    private float mapDimensions = 170.0f;

	[SerializeField]
	private Poolable powerUpPrefab;

	[SerializeField]
	private float powerUpSpawnDelay = 30.0f;
	[SerializeField]
	private Vector3 powerUpOffset;

	private Pool powerUpPool;

    private GameManager gameManager;

    // Bonus Spawner
    private IDisposable
        multiplierSpawner,
        bonusSpawner;
    
    
    #region MonoBehaviour
    private void Start () {
        ResetValues();
        
        poolManager = FindObjectOfType<PoolManager>();
        multiplierPool = poolManager.CreatePool("scoreMultipliers", 5, multiplierPrefab);
		powerUpPool = poolManager.CreatePool("powerUps", 3, powerUpPrefab);

        ResetTexts();

        gameManager = FindObjectOfType<GameManager>();
        gameManager.OnGameBegin.Subscribe(_ => {
            RegisterMessages();
            BeginSpawnBonuses();
        });
        gameManager.OnGameEnd.Subscribe(_ => {
            ResetValues();
            ResetTexts();
            EndSpawnBonuses();
            UnregisterMessages();
        });
    }
    
    private void OnDestroy()
    {
        if (MessagingCenter.Instance == null) { return; }

        UnregisterMessages();
    }
    #endregion MonoBehaviour
    #region Handlers
    private void HandlePlayerDeath(object obj)
    {
        if (obj is object[] == false) {
            if (debug) {
                Debug.LogError("Wrong arguments passed.");
            }
            return;
        }

        // Cast to retrieve arguments from polyvalent object[]
        var objs = (object[])obj;
        var id = (int)(objs[0]) - 1;
        var pos = (Vector3)objs[1];
        Debug.LogFormat("Player dead: {0}", id);
        // Update Death counter
        playerCachedDeath[id]++;
        UpdateDeathText(id);

        // Penalties on dead player!
        int retr = (int)(playerScoreBonusMultiplier[id] * multLostPercentOnDeath);
        playerScoreBonusMultiplier[id] -= retr;
        UpdatePlayerScoreMult(id);

        if (retr == 0) { return; }
        Vector3 rnd = Quaternion.Euler(0, UnityEngine.Random.Range(0.0f, 360.0f), 0.0f) * Vector3.right * 10.0f;
        multiplierPool.Spawn(new object[] { pos + rnd, retr * powerUpPerPoint });
    }

    private void HandlePlayerScoreMultiplier(object obj)
    {
        if (obj is object[] == false)
        {
            if (debug)
            {
                Debug.LogError("Wrong arguments passed.");
            }
            return;
        }
        var nobj = (object[])obj;
        var id = (int)nobj[0] - 1;
        var val = (int)nobj[1];
        playerPowerUpJauge[id] += val;
        if (playerPowerUpJauge[id] >= powerUpPerPoint)
        {
            playerScoreBonusMultiplier[id]++;
            UpdatePlayerScoreMult(id);
            playerPowerUpJauge[id] -= powerUpPerPoint;
            UpdateDeathText(id);
        }
        UpdateGauge(id);
    }

    private void UpdatePlayerScoreMult(int id)
    {
        string str = "000" + playerScoreBonusMultiplier[id].ToString();
        playerMultText[id].text = str.Substring(str.Length - 3);
    }

    private void UpdateGauge(int id)
    {
        gauges[id].fillAmount = ((float)playerPowerUpJauge[id] / (float)powerUpPerPoint);
    }

    private void UpdateDeathText(int id)
    {
        if (playerDeath == null || playerDeath.Length == 0 || playerDeath.Length < id) { return; }
        string str = "000" + playerCachedDeath[id];
        playerDeath[id].text = str.Substring(str.Length - 3);
    }

    private void HandleUnitTookDamage(object obj)
    {
        if (obj is object[] == false)
        {
            if (debug)
            {
                Debug.LogError("Wrong arguments passed.");
            }
            return;
        }
        HandleGainScore(ObjectToValues(obj));
    }

    private void HandleUnitKilled(object obj)
    {
        if (obj is object[] == false)
        {
            if (debug)
            {
                Debug.LogError("Wrong arguments passed.");
            }
            return;
        }
        HandleGainScore(ObjectToValues(obj));
    }

    private void HandleGainScore(object obj)
    {
        if (obj is object[] == false)
        {
            if (debug)
            {
                Debug.LogError("Wrong arguments passed.");
            }
            return;
        }
        HandleGainScore(ObjectToValues(obj));
    }

    private void HandleGainScore(Tuple<int, int> values)
    {
        playerCachedScore[values.Item1] += values.Item2 * playerScoreBonusMultiplier[values.Item1];
        string str = ("000000" + playerCachedScore[values.Item1].ToString());
        str = str.Substring(str.Length - 6);
        playerScores[values.Item1].text = str;
		playerScores [values.Item1].SendMessage ("PlayAnim");
    }
    #endregion

    private Tuple<int, int> ObjectToValues(object obj)
    {
        var nobj = (object[])obj;
        var id = (int)nobj[0] - 1;
        var score = (int)nobj[1];
        return new Tuple<int, int>(id, score);
    }

    private void ResetValues() {
        playerScoreBonusMultiplier = new[] { 1, 1, 1, 1 };
        playerCachedScore = new[] { 0, 0, 0, 0 };
        playerCachedDeath = new[] { 0, 0, 0, 0 };
        playerPowerUpJauge = new[] { 0, 0, 0, 0 };
    }

    private void RegisterMessages() {
        MessagingCenter.Instance.RegisterMessage("UnitKilled", HandleUnitKilled);
        MessagingCenter.Instance.RegisterMessage("UnitTookDamage", HandleUnitTookDamage);
        MessagingCenter.Instance.RegisterMessage("AddPlayerScoreMultiplier", HandlePlayerScoreMultiplier);
        MessagingCenter.Instance.RegisterMessage("PlayerGainScore", HandleGainScore);
        MessagingCenter.Instance.RegisterMessage("PlayerDeath", HandlePlayerDeath);
    }

    private void UnregisterMessages() {
        MessagingCenter.Instance.UnregisterMessage(
            "UnitKilled",
            "UnitTookDamage",
            "AddPlayerScoreMultiplier",
            "PlayerGainScore",
            "PlayerDeath");
    }

    private void BeginSpawnBonuses() {
        multiplierSpawner = Observable.Interval(TimeSpan.FromSeconds(15.0))
            .Where(_ => GameManager.Instance.IsInGame == true)
            .Where(_ => multiplierPool.empty == false)
            .Subscribe(_ => {
                Vector3 pos = Quaternion.Euler(0.0f, UnityEngine.Random.Range(0.0f, 360.0f), 0.0f) * Vector3.right
                    * UnityEngine.Random.Range(15.0f, mapDimensions);

                multiplierPool.Spawn(
                    new object[] { pos, 30 });
            })
            .AddTo(this);
        bonusSpawner = Observable.Interval(TimeSpan.FromSeconds(powerUpSpawnDelay))
            .Where(_ => GameManager.Instance.IsInGame == true)
            .Where(_ => powerUpPool.empty == false)
            .Subscribe(_ => {
                Vector3 pos = Quaternion.Euler(0.0f, UnityEngine.Random.Range(0.0f, 360.0f), 0.0f) * Vector3.right
                    * UnityEngine.Random.Range(15.0f, mapDimensions);

                powerUpPool.Spawn(pos + powerUpOffset);
            })
            .AddTo(this);
    }

    private void EndSpawnBonuses() {
        multiplierSpawner.Dispose();
        multiplierSpawner = null;

        bonusSpawner.Dispose();
        bonusSpawner = null;
    }

    private void ResetTexts() {
        int i, max;
        for (i = 0, max = 4; i < max; ++i) {
            UpdateDeathText(i);
            UpdateGauge(i);
            UpdatePlayerScoreMult(i);
        }
    }
}
