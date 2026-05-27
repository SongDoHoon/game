using System.Collections;
using UnityEngine;

public class BasicAttackAreaIndicator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Coroutine autoReleaseCoroutine;
    private BasicAttackAreaIndicatorPool ownerPool;
    private bool isReleased = true;

    public void Initialize(BasicAttackAreaIndicatorPool pool)
    {
        ownerPool = pool;
        CacheComponents();
    }

    public void Prepare(
        Vector3 position,
        Vector3 scale,
        Sprite sprite,
        Color color,
        int sortingOrder)
    {
        StopAutoRelease();
        CacheComponents();

        transform.position = position;
        transform.rotation = Quaternion.identity;
        transform.localScale = scale;
        isReleased = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
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
    }
}
