using UnityEngine;

// The opening at the edge of a room: walk onto it and you cross into the neighbouring room (no button),
// with a quick fade to hide the load. A map marks one with "<symbol> = edge <Scene> [<Spawn>]" (MapFile.cs),
// on a floor tile in the room's border. Like every door it saves, and makes an arrival spot beside itself
// ("From<Scene>"), so two rooms with edges to each other need nothing else.
public class RoomEdge : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawn;

    private bool used;

    public string TargetScene => targetScene;

    private void OnTriggerEnter(Collider other)
    {
        if (used || other.GetComponent<PlayerController>() == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct) return;
        Cross();
    }

    public void Cross()
    {
        if (used) return;
        used = true;
        GameSession.NextSpawn = targetSpawn;
        SaveSystem.Autosave(targetScene, targetSpawn); // every crossing is a save point
        ScreenFade.GoTo(targetScene);
    }
}
