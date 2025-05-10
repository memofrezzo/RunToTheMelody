using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LightItem
{
    [Tooltip("Referencia a la luz que se controlará")]
    public Light lightReference;

    [Tooltip("Primer color de la transición (por defecto: 630000)")]
    public Color color1 = new Color32(0x63, 0x00, 0x00, 255);

    [Tooltip("Segundo color de la transición (por defecto: FF0000)")]
    public Color color2 = new Color32(0xFF, 0x00, 0x00, 255);

    [Tooltip("Tiempo en segundos para completar la transición (por defecto: 0.244)")]
    public float transitionTime = 0.244f;

    [Tooltip("Valor entre 0 y 1: 0 = cambio instantáneo, 1 = transición completa (por defecto: 0)")]
    [Range(0f, 1f)]
    public float lerpFactor = 0f;
}

public class LightManager : MonoBehaviour
{
    [Tooltip("Lista de luces con sus configuraciones de color")]
    public List<LightItem> lights = new List<LightItem>();

    private void Update()
    {
        foreach (LightItem item in lights)
        {
            if (item.lightReference != null && item.transitionTime > 0f)
            {
                float t = Mathf.PingPong(Time.time, item.transitionTime) / item.transitionTime;
                if (item.lerpFactor <= 0f)
                {
                    // Cambio instantáneo en el punto medio
                    item.lightReference.color = (t < 0.5f) ? item.color1 : item.color2;
                }
                else
                {
                    // Transición suave usando Lerp
                    item.lightReference.color = Color.Lerp(item.color1, item.color2, t * item.lerpFactor);
                }
            }
        }
    }
}
