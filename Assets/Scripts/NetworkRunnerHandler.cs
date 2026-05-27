using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;


public class NetworkRunnerHandler :
    MonoBehaviour,
    INetworkRunnerCallbacks
{
    [Header("Network")]
    public NetworkPrefabRef playerPrefab;

    private NetworkRunner runner;

    [Header("Loading UI")]
    public LoadingUI loadingUI;

    public GameObject loadingPanel;

    async void Awake()
    {
        Debug.Log(
            "NETWORK START"
        );

        loadingPanel.SetActive(true);

        StartCoroutine(
            FakeLoading()
        );

        runner =
            GetComponent<NetworkRunner>();

        if (runner == null)
        {
            runner =
                gameObject.AddComponent
                <NetworkRunner>();
        }

        runner.ProvideInput = true;

        runner.AddCallbacks(this);

        NetworkSceneManagerDefault
            sceneManager =
                gameObject.AddComponent
                <NetworkSceneManagerDefault>();

        try
        {
            Debug.Log(
                "START GAME BEGIN"
            );

            var result =
                await runner.StartGame(
                    new StartGameArgs()
                    {
                        GameMode =
                            GameMode.AutoHostOrClient,

                        SessionName =
                            "Room1",

                        Scene =
                            SceneRef.FromIndex(
                                SceneManager
                                .GetActiveScene()
                                .buildIndex
                            ),

                        SceneManager =
                            sceneManager
                    }
                );

            Debug.Log(
                "START GAME RESULT : "
                + result.Ok
            );

            if (result.Ok)
            {
                loadingUI.SetProgress(1f);

                await System.Threading.Tasks
                    .Task.Delay(300);

                loadingPanel.SetActive(false);
            }
            else
            {
                Debug.LogError(
                    result.ShutdownReason
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    IEnumerator FakeLoading()
    {
        float progress = 0f;

        while (progress < 0.9f)
        {
            progress +=
                Time.deltaTime * 0.3f;

            loadingUI.SetProgress(
                progress
            );

            yield return null;
        }
    }

    public void OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player
    )
    {
        Debug.Log(
            "=== PLAYER JOINED ==="
        );

        if (!runner.IsServer)
            return;

        Vector2 spawnPosition =
            GetRandomSpawnPosition();

        Debug.Log(
            "SPAWN START"
        );

        runner.Spawn(
            playerPrefab,
            spawnPosition,
            Quaternion.identity,
            player
        );

        Debug.Log(
            "SPAWN SUCCESS"
        );
    }

    public void OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player
    )
    {
    }

    Vector2 GetRandomSpawnPosition()
    {
        float x =
            UnityEngine.Random.Range(
                -720f,
                720f
            );

        float y =
            UnityEngine.Random.Range(
                -360f,
                360f
            );

        return new Vector2(x, y);
    }

    public void OnInput(
        NetworkRunner runner,
        NetworkInput input
    )
    {
    }

    public void OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input
    )
    {
    }

    public void OnShutdown(
        NetworkRunner runner,
        ShutdownReason shutdownReason
    )
    {
    }

    public void OnConnectedToServer(
        NetworkRunner runner
    )
    {
    }

    public void OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason
    )
    {
    }

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs
        .ConnectRequest request,
        byte[] token
    )
    {
    }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason
    )
    {
    }

    public void OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message
    )
    {
    }

    public void OnSessionListUpdated(
        NetworkRunner runner,
        List<SessionInfo> sessionList
    )
    {
    }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object>
        data
    )
    {
    }

    public void OnHostMigration(
        NetworkRunner runner,
        HostMigrationToken
        hostMigrationToken
    )
    {
    }

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ArraySegment<byte> data
    )
    {
    }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress
    )
    {
    }

    public void OnSceneLoadDone(
        NetworkRunner runner
    )
    {
    }

    public void OnSceneLoadStart(
        NetworkRunner runner
    )
    {
    }

    public void OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player
    )
    {
    }

    public void OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player
    )
    {
    }
}