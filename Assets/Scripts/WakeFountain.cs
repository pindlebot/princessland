using UnityEngine;

// A fountain the hero has walked past becomes the place she wakes up after a Gentle Mode
// nap (instead of where she came into the area). The full "fountain = save point" idea
// comes later; this is the first half of it.
public class WakeFountain : MonoBehaviour
{
    [SerializeField] private float reach = 4f;
    [SerializeField] private Transform wakeSpot; // just in front of the basin

    public bool IsWakePoint => LevelBootstrap.Current != null && LevelBootstrap.Current.WakePoint == wakeSpot;

    private void Update()
    {
        var bootstrap = LevelBootstrap.Current;
        if (bootstrap == null || bootstrap.Player == null || IsWakePoint) return;
        Vector3 offset = bootstrap.Player.transform.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= reach * reach) bootstrap.WakePoint = wakeSpot;
    }
}
