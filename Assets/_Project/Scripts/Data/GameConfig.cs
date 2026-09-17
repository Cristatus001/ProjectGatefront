using UnityEngine;

namespace ProjectGatefront.Data
{
    public abstract class GameConfig : ScriptableObject
    {
        [SerializeField] private string designerNotes;

        public string DesignerNotes => designerNotes;
    }
}
