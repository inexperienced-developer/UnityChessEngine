using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Private", fileName = "New Private Settings")]
public class PrivateSettings : ScriptableObject
{
    [SerializeField] private LayerMask m_pieceMask;
    [SerializeField] private LayerMask m_posMask;
    public LayerMask PieceMask => m_pieceMask;
    public LayerMask PosMask => m_posMask;
}
