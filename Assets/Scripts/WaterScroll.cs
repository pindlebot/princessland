using UnityEngine;

// Makes water look alive by sliding its texture slowly. Uses a MaterialPropertyBlock, so the
// shared material asset isn't changed (and every water tile drifts in step).
[RequireComponent(typeof(Renderer))]
public class WaterScroll : MonoBehaviour
{
    [SerializeField] private Vector2 speed = new Vector2(0.03f, 0.015f);

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
