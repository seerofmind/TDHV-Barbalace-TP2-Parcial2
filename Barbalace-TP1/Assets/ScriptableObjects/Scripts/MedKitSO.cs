using UnityEngine;

[CreateAssetMenu(fileName = "MedKitSO", menuName = "Scriptable Objects/MedKitSO")]
public class MedKitSO : ScriptableObject
{
    [Header("Item Settings")]
    public int healthToRestore = 50;
}
