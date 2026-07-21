using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.Editor
{
    [CustomEditor(typeof(Customer), true)]
    public class CustomerEditor : UnityEditor.Editor
    {
        private const string HAND_ATTACHMENTS_PREFAB_PATH = "Assets/CheckoutFrenzy/Prefabs/Customers/HandAttachments.prefab";
        private const string OVERHEAD_UI_PREFAB_PATH = "Assets/CheckoutFrenzy/Prefabs/Customers/OverheadUI.prefab";
        private const float OVERHEAD_HEIGHT_OFFSET = 0.6f;

        SerializedProperty trait;
        SerializedProperty handAttachments;
        SerializedProperty overheadUI;

        private void OnEnable()
        {
            trait = serializedObject.FindProperty("trait");
            handAttachments = serializedObject.FindProperty("handAttachments");
            overheadUI = serializedObject.FindProperty("overheadUI");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(trait);
            if (trait.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Customer Trait is missing! This NPC needs a Trait to define its behavior.", MessageType.Error);
            }
            EditorGUILayout.Space();

            if (handAttachments == null || handAttachments.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("HandAttachments is missing. Please load the appropriate prefab in prefab mode.", MessageType.Error);

                bool inPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

                GUI.enabled = inPrefabMode;

                if (GUILayout.Button("Load HandAttachments"))
                {
                    LoadHandAttachments();
                }

                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.HelpBox("HandAttachments is properly assigned. Adjust the position as you see fit.", MessageType.Info);

                bool inPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

                GUI.enabled = inPrefabMode;

                if (GUILayout.Button("Select HandAttachments"))
                {
                    Selection.activeObject = handAttachments.objectReferenceValue;
                }

                GUI.enabled = true;
            }

            if (overheadUI == null || overheadUI.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("OverheadUI is missing. Please load the appropriate prefab in prefab mode.", MessageType.Error);

                bool inPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

                GUI.enabled = inPrefabMode;

                if (GUILayout.Button("Load OverheadUI"))
                {
                    LoadOverheadUI();
                }

                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.HelpBox("OverheadUI is properly assigned. Adjust the position as you see fit.", MessageType.Info);

                bool inPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

                GUI.enabled = inPrefabMode;

                if (GUILayout.Button("Select OverheadUI"))
                {
                    Selection.activeObject = overheadUI.objectReferenceValue;
                }

                GUI.enabled = true;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void LoadHandAttachments()
        {
            Customer customer = (Customer)target;
            Animator animator = customer.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator not found on the Customer object.");
                return;
            }

            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand == null)
            {
                Debug.LogError("Right hand bone not found.");
                return;
            }

            GameObject handAttachmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HAND_ATTACHMENTS_PREFAB_PATH);

            if (handAttachmentPrefab == null)
            {
                Debug.LogError("HandAttachments prefab not found at path: " + HAND_ATTACHMENTS_PREFAB_PATH);
                return;
            }

            GameObject handAttachmentInstance = PrefabUtility.InstantiatePrefab(handAttachmentPrefab, rightHand) as GameObject;

            handAttachmentInstance.transform.localPosition = Vector3.zero;
            handAttachmentInstance.transform.localRotation = Quaternion.identity;
            handAttachmentInstance.transform.localScale = Vector3.one;

            handAttachments.objectReferenceValue = handAttachmentInstance;

            EditorUtility.SetDirty(customer);
            EditorSceneManager.MarkSceneDirty(customer.gameObject.scene);

            serializedObject.ApplyModifiedProperties();
        }

        private void LoadOverheadUI()
        {
            Customer customer = (Customer)target;
            Animator animator = customer.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator not found on the Customer object.");
                return;
            }

            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head == null)
            {
                Debug.LogError("Head bone not found.");
                return;
            }

            GameObject overheadUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OVERHEAD_UI_PREFAB_PATH);

            if (overheadUIPrefab == null)
            {
                Debug.LogError("OverheadUI prefab not found at path: " + OVERHEAD_UI_PREFAB_PATH);
                return;
            }

            GameObject overheadUIInstance = PrefabUtility.InstantiatePrefab(overheadUIPrefab, head) as GameObject;

            overheadUIInstance.transform.localPosition = Vector3.up * OVERHEAD_HEIGHT_OFFSET;
            overheadUIInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            overheadUIInstance.transform.localScale = Vector3.one;

            overheadUI.objectReferenceValue = overheadUIInstance;

            EditorUtility.SetDirty(customer);
            EditorSceneManager.MarkSceneDirty(customer.gameObject.scene);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
