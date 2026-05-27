using System.Collections;
using UnityEngine;

public class SkillIndicator : MonoBehaviour
{
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    private SpriteRenderer spriteRenderer;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh coneMesh;
    private Vector3[] coneVertices;
    private int[] coneTriangles;
    private MaterialPropertyBlock materialPropertyBlock;
    private Coroutine autoReleaseCoroutine;
    private SkillIndicatorPool ownerPool;
    private bool isReleased = true;

    public void Initialize(SkillIndicatorPool pool)
    {
        ownerPool = pool;
        CacheComponents();
    }

    public void PrepareLine(
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Sprite sprite,
        Color color,
        int sortingOrder)
    {
        StopAutoRelease();
        CacheComponents();

        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
        isReleased = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (meshRenderer != null)
            meshRenderer.enabled = false;
    }

    public void PrepareCone(
        Vector3 position,
        Vector3 forward,
        float range,
        float halfAngle,
        Color color,
        int sortingOrder,
        int segmentCount,
        Material sharedMaterial)
    {
        StopAutoRelease();
        CacheComponents();

        transform.position = position;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        isReleased = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        EnsureConeMesh(segmentCount);
        RebuildConeMesh(forward.normalized, range, halfAngle, segmentCount);

        if (meshFilter != null)
            meshFilter.sharedMesh = coneMesh;

        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
            meshRenderer.sharedMaterial = sharedMaterial;
            meshRenderer.sortingOrder = sortingOrder;

            if (materialPropertyBlock == null)
                materialPropertyBlock = new MaterialPropertyBlock();

            materialPropertyBlock.Clear();
            materialPropertyBlock.SetColor(ColorPropertyId, color);
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

    public void StartAutoRelease(float duration)
    {
        StopAutoRelease();
        autoReleaseCoroutine = StartCoroutine(CoAutoRelease(duration));
    }

    public void CleanupForPool()
    {
        StopAutoRelease();
        CacheComponents();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            spriteRenderer.sprite = null;
            spriteRenderer.color = Color.clear;
        }

        if (meshRenderer != null)
        {
            meshRenderer.SetPropertyBlock(null);
            meshRenderer.enabled = false;
        }

        if (meshFilter != null)
            meshFilter.sharedMesh = null;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public bool TryMarkReleased()
    {
        if (isReleased)
            return false;

        isReleased = true;
        return true;
    }

    private IEnumerator CoAutoRelease(float duration)
    {
        yield return new WaitForSeconds(duration);
        autoReleaseCoroutine = null;

        if (ownerPool != null)
            ownerPool.Release(this);
    }

    private void StopAutoRelease()
    {
        if (autoReleaseCoroutine == null)
            return;

        StopCoroutine(autoReleaseCoroutine);
        autoReleaseCoroutine = null;
    }

    private void CacheComponents()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
    }

    private void EnsureConeMesh(int segmentCount)
    {
        int vertexCount = segmentCount + 2;
        int triangleIndexCount = segmentCount * 3;

        if (coneMesh == null)
        {
            coneMesh = new Mesh
            {
                name = "ConeSkillIndicatorMesh"
            };
        }

        if (coneVertices == null || coneVertices.Length != vertexCount)
            coneVertices = new Vector3[vertexCount];

        if (coneTriangles == null || coneTriangles.Length != triangleIndexCount)
            coneTriangles = new int[triangleIndexCount];
    }

    private void RebuildConeMesh(Vector3 forward, float range, float halfAngle, int segmentCount)
    {
        coneMesh.Clear();

        coneVertices[0] = Vector3.zero;
        float forwardAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        float startAngle = forwardAngle - halfAngle;
        float angleStep = (halfAngle * 2f) / segmentCount;

        for (int i = 0; i <= segmentCount; i++)
        {
            float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            coneVertices[i + 1] = new Vector3(Mathf.Cos(angle) * range, Mathf.Sin(angle) * range, 0f);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int triangleIndex = i * 3;
            coneTriangles[triangleIndex] = 0;
            coneTriangles[triangleIndex + 1] = i + 1;
            coneTriangles[triangleIndex + 2] = i + 2;
        }

        coneMesh.vertices = coneVertices;
        coneMesh.triangles = coneTriangles;
        coneMesh.RecalculateBounds();
    }
}
