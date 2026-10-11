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

    // The texture's tiling and offset: _BaseMap_ST on the Universal Render Pipeline's Lit shader (_MainTex_ST on the old Standard one).
    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
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
        var scrolled = new Vector4(tiling.x, tiling.y, offset.x % 1f, offset.y % 1f);
        block.SetVector(BaseMapST, scrolled);
        block.SetVector(MainTexST, scrolled);
        rend.SetPropertyBlock(block);
    }
}
