using UnityEngine;
using UnityEngine.SceneManagement;

public enum WorldState { Reality, Nightmare, Station }

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }
    public WorldState CurrentWorld { get; private set; }
    
    [SerializeField] private string realityScene = "Apartment";
    [SerializeField] private string[] nightmareScenes = { "Forest", "Hospital", "ChildhoodHome" };
    [SerializeField] private string stationScene = "Station";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        CurrentWorld = WorldState.Reality;
    }

    public void TransitionToWorld(WorldState targetWorld, string specificScene = "")
    {
        CurrentWorld = targetWorld;
        string sceneToLoad = targetWorld switch
        {
            WorldState.Reality => realityScene,
            WorldState.Nightmare => specificScene != "" ? specificScene : nightmareScenes[0],
            WorldState.Station => stationScene,
            _ => realityScene
        };

        SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
    }

    public void ApplyWorldEffect(GameObject obj, WorldState fromWorld, WorldState toWorld)
    {
        // Example: Modify object appearance based on world transition
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = toWorld == WorldState.Nightmare ? Color.red : Color.white;
        }
    }
}