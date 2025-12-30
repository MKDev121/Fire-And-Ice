using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Threading.Tasks;
public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
  [SerializeField] private NetworkPrefabRef _playerPrefab;
  private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
  int count;
  bool spawned;
  public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
  {
    // Create a unique position for the player
    if (runner.IsServer)
    {
      Vector3 spawnPosition = new Vector3(player.RawEncoded % runner.Config.Simulation.PlayerCount * 3, 1, 0);

      NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

      // Keep track of the player avatars for easy access
      _spawnedCharacters.Add(player, networkPlayerObject);
      networkPlayerObject.GetComponent<Player>().index = count;
      count++;

    }


  }
  public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
  {
    if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
    {
      runner.Despawn(networkObject);
      _spawnedCharacters.Remove(player);
    }
  }
  public void OnInput(NetworkRunner runner, NetworkInput input)
  {
    var data = new NetworkInputData();


    // if (Input.GetKey(KeyCode.S))
    //   data.direction += Vector3.back;
    data.buttons.Set(MyButtons.Jump, Input.GetKey(KeyCode.W));
    data.buttons.Set(MyButtons.Attack, Input.GetMouseButton(0));

    if (Input.GetKey(KeyCode.A))
      data.direction += -1f;

    if (Input.GetKey(KeyCode.D))
      data.direction += 1f;

    input.Set(data);

  }
  public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
  public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
  public void OnConnectedToServer(NetworkRunner runner) { }
  public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { GameObject.Find("GameOverScreen").SetActive(false); SceneManager.LoadScene(0); }
  public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
  public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
  public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
  public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
  public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
  public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
  public void OnSceneLoadDone(NetworkRunner runner)
  {
    if (_runner.IsServer)
      GameObject.Find("EnemyManager").GetComponent<EnemyManager>().StartTimer(_runner);
  }
  public void OnSceneLoadStart(NetworkRunner runner) { }
  public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
  public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
  public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
  public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

  private NetworkRunner _runner;
  bool jump;

  async void StartGame(GameMode mode)
  {
    // Create the Fusion runner and let it know that we will be providing user input
    _runner = gameObject.AddComponent<NetworkRunner>();
    _runner.ProvideInput = true;

    var runnerSimulatePhysics3D = gameObject.AddComponent<RunnerSimulatePhysics3D>();
    runnerSimulatePhysics3D.ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateAlways;

    var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
    var sceneInfo = new NetworkSceneInfo();
    if (scene.IsValid)
      sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);

    await _runner.StartGame(new StartGameArgs()
    {
      GameMode = mode,
      SessionName = "TestRoom",
      Scene = scene,
      SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
    }
    );


  }

  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }


  private void OnGUI()
  {
    if (_runner == null)
    {
      if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
      {
        StartGame(GameMode.Host);
      }
      if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
      {
        StartGame(GameMode.Client);
      }
    }
  }
  public async void DisconnectPlayers()
  {

    if (_runner.IsServer)
    {
      List<PlayerRef> list = _spawnedCharacters.Keys.ToList<PlayerRef>();
      list.Reverse();
      foreach (PlayerRef player in list)
      {
        _spawnedCharacters.Remove(player);
        _runner.Disconnect(player);

      }
      // EnemyManager enemyManager = GameObject.Find("EnemyManager").GetComponent<EnemyManager>();
      // enemyManager.gameOver();

    }
    await _runner.Shutdown();
    SceneManager.LoadScene(0);

  }
}
