
using UnityEngine;

// [CreateAssetMenu(menuName = "ScriptableObjects/GameInfo")]
public class GameInfo : ScriptableObject
{ //Objeto unico con informacion serializable sobre configuracion del juego, referenciada por el GameManager
    [field:SerializeField] public uint InitialPeople { get; private set; }

    [field:Header("Pedestrian AI")]
    [field:SerializeField] public uint InitialPedestrians { get; private set; }
    [field:SerializeField][field:Range(0f, 1f)] public float DespawnProbability { get; private set; }
    [field:SerializeField] public float MinAgentSpeed { get; private set; }
    [field:SerializeField] public float MaxAgentSpeed { get; private set; }
    
}
