using System.Collections.Generic;
using Humanoid_Basics.Core.Scriptables;
using UnityEditor;
using UnityEditorInternal;

namespace Humanoid_Basics.Editor.Scripts.ScriptableObjects
{
    
    [CustomEditor(typeof(InventoryDatabase)), CanEditMultipleObjects]
    public class InventoryDatabaseEditor : UnityEditor.Editor
    {
        public InventoryDatabase database;
        public ReorderableList list;
        public SerializedProperty itemDb;
        
        private void OnEnable()
        {
            database = target as InventoryDatabase;
            itemDb = serializedObject.FindProperty("itemDatabase");
        }
        
        private void OnAdd()
        {
            var itemMapper = new InventoryDatabase.Item
            {
                name = "New Item"
            };
            database.itemDatabase.Add(itemMapper);
            database.Reseed();
        }
        
        private void OnRemove(IReadOnlyList<int> ids)
        {
            foreach (var t in ids)
            {
                database.itemDatabase.RemoveAt(t);
            }
            database.Reseed();
        }
    }
}
