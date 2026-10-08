using UnityEngine;

// A playable character, as data: what the select screen shows and which player prefab
// to spawn. Lives in Assets/Characters; make more with Create > Dungeon > Character.
[CreateAssetMenu(menuName = "Dungeon/Character", fileName = "NewCharacter")]
public class CharacterDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "New Hero";
    [TextArea] [SerializeField] private string description;
    [SerializeField] private Sprite portrait;
    [SerializeField] private GameObject prefab;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Portrait => portrait;
    public GameObject Prefab => prefab;
}
