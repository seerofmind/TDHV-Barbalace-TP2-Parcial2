using UnityEngine;

[CreateAssetMenu(fileName = "SurveillanceCamera", menuName = "Scriptable Objects/SurveillanceCamera")]
public class CameraData : ScriptableObject
{
    [Header("Surveillance Camera Stats")]
    public float Health = 100f;
    public float ViewAngle = 60f;
    public float ViewRange = 5f;
}
