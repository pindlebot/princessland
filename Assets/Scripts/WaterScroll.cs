using UnityEngine;

// Makes water look alive by sliding its texture slowly. Uses a MaterialPropertyBlock, so the
// shared material asset isn't changed (and every water tile drifts in step).
[RequireComponent(typeof(Renderer))]
public class WaterScroll : MonoBehaviour
{
    // Very slow: about one texel every three seconds, so the glints step along calmly.
    [SerializeField] private Vector2 speed = new Vector2(0.01f, 0.005f);
    // How many times the texture repeats across the surface (a waterfall repeats once a metre).
    [SerializeField] private Vector2 tiling = Vector2.one;

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
        block.SetVector(MainTexST, new Vector4(tiling.x, tiling.y, offset.x % 1f, offset.y % 1f));
        rend.SetPropertyBlock(block);
    }
}
