using UnityEngine;

// Makes water look alive by sliding its texture slowly. Uses a MaterialPropertyBlock, so the
// shared material asset isn't changed (and every water tile drifts in step).
[RequireComponent(typeof(Renderer))]
public class WaterScroll : MonoBehaviour
{
    // Very slow: about one texel every three seconds, so the glints step along calmly.
    [SerializeField] private Vector2 speed = new Vector2(0.01f, 0.005f);

    private static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");
    private Renderer rend;
    private MaterialPropertyBlock block;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        Vector2 offset = speed * Time.time;
        block.SetVector(MainTexST, new Vector4(1f, 1f, offset.x % 1f, offset.y % 1f));
        rend.SetPropertyBlock(block);
    }
}
