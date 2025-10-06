using UnityEngine;

public class CrushEffect : MonoBehaviour
{
    public void OnDestroy()
    {
        Destroy(gameObject);
    }
}
