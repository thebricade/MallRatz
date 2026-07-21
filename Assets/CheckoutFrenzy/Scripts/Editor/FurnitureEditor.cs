using System.IO;
using UnityEditor;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.Editor
{
    [CustomEditor(typeof(Furniture), true)]
    [CanEditMultipleObjects]
    public class FurnitureEditor : UnityEditor.Editor
    {
        private SerializedProperty furnitureId;
        private SerializedProperty icon;

        private Furniture[] furnitures;
        private Vector3 iconOrientation = new Vector3(0f, 150f, 0f);

        private void OnEnable()
        {
            furnitures = Resources.LoadAll<Furniture>("Furnitures");

            furnitureId = serializedObject.FindProperty("furnitureId");
            icon = serializedObject.FindProperty("icon");

            if (furnitureId.intValue == 0)
            {
                furnitureId.intValue = GetNextFurnitureId();
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Furniture Details", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Furniture ID", furnitureId.intValue.ToString());

            if (HasDuplicateFurnitureId(furnitureId.intValue))
            {
                EditorGUILayout.HelpBox("Duplicate Furniture ID detected. Click 'Fix' to resolve.", MessageType.Warning);

                if (GUILayout.Button("Fix Duplicate Furniture ID"))
                {
                    furnitureId.intValue = GetNextFurnitureId();
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Icon Generator", EditorStyles.boldLabel);

            if (icon.objectReferenceValue != null)
            {
                EditorGUILayout.Space();
                Sprite sprite = icon.objectReferenceValue as Sprite;
                if (sprite != null)
                {
                    const float previewSize = 128f;

                    Rect rect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));

                    Texture2D previewTexture = AssetPreview.GetAssetPreview(sprite) ?? sprite.texture;

                    if (previewTexture != null)
                    {
                        GUI.Box(rect, GUIContent.none);
                        GUI.DrawTexture(rect, previewTexture, ScaleMode.ScaleToFit, true);
                    }
                }
            }

            iconOrientation = EditorGUILayout.Vector3Field("Icon Orientation", iconOrientation);

            if (GUILayout.Button(icon.objectReferenceValue == null ? "Generate Icon" : "Regenerate Icon"))
            {
                GenerateIconFromGameObject();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void GenerateIconFromGameObject()
        {
            Furniture furniture = (Furniture)target;

            // 1. Create a temporary clone of the furniture GameObject
            GameObject clone = Instantiate(furniture.gameObject);
            clone.transform.position = new Vector3(0, -1000, 0);
            clone.transform.eulerAngles = iconOrientation;

            DestroyImmediate(clone.GetComponent<Furniture>());

            // 2. Set up the Camera
            Camera camera = new GameObject("TempCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Depth;
            camera.nearClipPlane = 0.01f;
            camera.orthographic = true;
            camera.transform.rotation = Quaternion.Euler(15f, 0, 0);

            // 3. Calculate Bounds
            Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError("No renderers found on the furniture. Cannot generate icon.");
                DestroyImmediate(clone);
                DestroyImmediate(camera.gameObject);
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            // 4. Frame the Camera
            Vector3[] worldCorners = new Vector3[8];
            GetBoundsCorners(bounds, worldCorners);

            Matrix4x4 worldToCamera = camera.transform.worldToLocalMatrix;
            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;

            foreach (Vector3 corner in worldCorners)
            {
                Vector3 localCorner = worldToCamera.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, localCorner);
                max = Vector3.Max(max, localCorner);
            }

            Vector3 extents = (max - min) * 0.5f;
            camera.orthographicSize = Mathf.Max(extents.y, extents.x) * 1.05f;

            Vector3 offset = camera.transform.rotation * new Vector3(0, 0, -extents.z);
            camera.transform.position = bounds.center + offset - camera.transform.forward;

            // 5. Render to Texture
            RenderTexture renderTexture = new RenderTexture(256, 256, 16) { antiAliasing = 8 };
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();

            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            // 6. Save as PNG
            string directory = "Assets/CheckoutFrenzy/Sprites/Furnitures";
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            string cleanName = furniture.gameObject.name.Replace("(Clone)", "").Trim();
            string path = Path.Combine(directory, cleanName + "_Generated.png");

            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.Refresh();

            // 7. Import as Sprite and Assign
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            icon.objectReferenceValue = sprite;

            // 8. Cleanup
            DestroyImmediate(clone);
            DestroyImmediate(camera.gameObject);
            renderTexture.Release();

            Debug.Log("Furniture Icon generated and assigned successfully: " + path);
        }

        private void GetBoundsCorners(Bounds bounds, Vector3[] corners)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
            corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
            corners[3] = center + new Vector3(extents.x, extents.y, -extents.z);
            corners[4] = center + new Vector3(-extents.x, -extents.y, extents.z);
            corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
            corners[6] = center + new Vector3(-extents.x, extents.y, extents.z);
            corners[7] = center + new Vector3(extents.x, extents.y, extents.z);
        }

        private int GetNextFurnitureId()
        {
            int maxId = 0;

            foreach (var furniture in furnitures)
            {
                if (furniture != null)
                    maxId = Mathf.Max(maxId, furniture.FurnitureID);
            }

            return maxId + 1;
        }

        private bool HasDuplicateFurnitureId(int currentId)
        {
            if (currentId == 0) return false;

            int count = 0;

            foreach (var furniture in furnitures)
            {
                if (furniture != null && furniture.FurnitureID == currentId)
                {
                    count++;
                    if (count > 1)
                        return true;
                }
            }

            return false;
        }
    }
}
