using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.Editor
{
    [CustomEditor(typeof(ShelvingUnit), true)]
    public class ShelvingUnitEditor : FurnitureEditor
    {
        private const string SHELF_PREFAB_PATH = "Assets/CheckoutFrenzy/Prefabs/Shelf.prefab";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            bool inPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

            GUI.enabled = inPrefabMode;

            GUILayout.Space(10);

            if (GUILayout.Button("Add Shelf"))
            {
                AddShelf();
            }

            GUI.enabled = true;
        }

        private void AddShelf()
        {
            ShelvingUnit shelvingUnit = (ShelvingUnit)target;

            GameObject shelfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SHELF_PREFAB_PATH);

            if (shelfPrefab == null)
            {
                Debug.LogError("Shelf prefab not found at path: " + SHELF_PREFAB_PATH);
                return;
            }

            GameObject shelfInstance = PrefabUtility.InstantiatePrefab(shelfPrefab, shelvingUnit.transform) as GameObject;

            shelfInstance.transform.localPosition = Vector3.zero;
            shelfInstance.transform.localRotation = Quaternion.identity;
            shelfInstance.transform.localScale = Vector3.one;

            shelfInstance.name = "Shelf";

            EditorUtility.SetDirty(shelvingUnit);
            EditorSceneManager.MarkSceneDirty(shelvingUnit.gameObject.scene);
        }
    }
}
