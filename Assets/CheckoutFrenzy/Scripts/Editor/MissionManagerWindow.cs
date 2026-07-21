using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.Editor
{
    public class MissionManagerWindow : EditorWindow
    {
        private Mission[] missions;
        private Mission selectedMission;
        private Dictionary<Mission, string> missionToGUIDMap;

        private int currentPage = 0;
        private const int missionsPerPage = 10;

        [MenuItem("Tools/Checkout Frenzy/Mission Manager")]
        public static void ShowWindow()
        {
            GetWindow<MissionManagerWindow>("Mission Manager");
        }

        private void OnEnable()
        {
            LoadMissions();
        }

        private void LoadMissions()
        {
            missionToGUIDMap = new Dictionary<Mission, string>();

            // Find all GUIDs for Mission assets
            string[] missionGuids = AssetDatabase.FindAssets("t:Mission", new[] { "Assets/CheckoutFrenzy/Resources/Missions" });

            // Load missions and map them to their GUIDs
            missions = missionGuids
                .Select(guid =>
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Mission mission = AssetDatabase.LoadAssetAtPath<Mission>(path);
                    if (mission != null)
                    {
                        missionToGUIDMap[mission] = guid;
                    }
                    return mission;
                })
                .Where(mission => mission != null)
                .OrderBy(m => m.missionId)
                .ToArray();
        }

        private void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 30;
            titleStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f, 1f);

            GUILayout.Label("Mission Manager", titleStyle);

            GUILayout.BeginVertical(GUILayout.Height(210));
            DisplayMissionPage();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Height(50));
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Previous", GUILayout.Width(100)))
            {
                if (currentPage > 0) currentPage--;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Page {currentPage + 1}/{GetTotalPages()}");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Next", GUILayout.Width(100)))
            {
                if (currentPage < GetTotalPages() - 1) currentPage++;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            if (GUILayout.Button("Add New Mission"))
            {
                AddNewMission();
            }

            if (GUILayout.Button("Renumber All Missions"))
            {
                RenumberAllMissions();
            }

            if (selectedMission != null)
            {
                ShowMissionDetails();
            }
            else
            {
                GUIStyle helpStyle = new GUIStyle(GUI.skin.label);
                helpStyle.fontStyle = FontStyle.Italic;
                helpStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f, 1f);
                GUILayout.Label("Please select a misson to edit", helpStyle);
            }
        }

        private void DisplayMissionPage()
        {
            int totalPages = GetTotalPages();
            if (totalPages == 0) return;

            int startIndex = currentPage * missionsPerPage;
            int endIndex = Mathf.Min(startIndex + missionsPerPage, missions.Length);

            for (int i = startIndex; i < endIndex && i < missions.Length; i++)
            {
                Mission mission = missions[i];

                EditorGUILayout.BeginHorizontal();

                // Highlight if this mission is currently being edited
                if (selectedMission == mission)
                {
                    GUI.backgroundColor = Color.yellow; // Highlight color
                }

                // Display mission details
                if (GUILayout.Button($"Mission {mission.missionId} - {mission.goalType}"))
                {
                    if (mission == selectedMission) selectedMission = null;
                    else selectedMission = mission;
                }

                GUI.backgroundColor = Color.white; // Reset background color

                // Shift Up Button
                if (GUILayout.Button("↑", GUILayout.Width(30)))
                {
                    ShiftMission(i, -1);
                }

                // Shift Down Button
                if (GUILayout.Button("↓", GUILayout.Width(30)))
                {
                    ShiftMission(i, 1);
                }

                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    DeleteMission(mission);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private int GetTotalPages()
        {
            return Mathf.CeilToInt((float)missions.Length / missionsPerPage);
        }

        private void ShowMissionDetails()
        {
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            labelStyle.fontSize = 15;
            labelStyle.padding.left = 5;

            GUILayout.BeginVertical(GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Editing Mission {selectedMission.missionId:D3}", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            EditorGUI.indentLevel++;

            selectedMission.goalType = (MissionGoal)EditorGUILayout.EnumPopup("Goal Type", selectedMission.goalType);

            if (selectedMission.goalType == MissionGoal.Checkout || selectedMission.goalType == MissionGoal.Revenue)
            {
                selectedMission.targetId = 0;
            }
            else
            {
                if (selectedMission.goalType == MissionGoal.Sell || selectedMission.goalType == MissionGoal.Restock)
                {
                    ShowProductDropdown();
                }
                else if (selectedMission.goalType == MissionGoal.Furnish)
                {
                    ShowFurnitureDropdown();
                }
                else if (selectedMission.goalType == MissionGoal.License)
                {
                    ShowLicenseDropdown();
                }
            }

            if (selectedMission.goalType == MissionGoal.License)
            {
                selectedMission.goalAmount = 1;
            }
            else
            {
                string goalAmountLabel = "Goal Amount";

                if (selectedMission.goalType == MissionGoal.Revenue) goalAmountLabel += " (in cents)";
                else if (selectedMission.goalType == MissionGoal.Restock) goalAmountLabel += " (in box)";

                selectedMission.goalAmount = EditorGUILayout.IntField(goalAmountLabel, selectedMission.goalAmount);
            }

            selectedMission.reward = EditorGUILayout.IntField("Reward", selectedMission.reward);

            EditorUtility.SetDirty(selectedMission);
        }

        private void ShowProductDropdown()
        {
            Product[] products = Resources.LoadAll<Product>("Products");
            string[] productNames = new string[products.Length];
            int[] productIds = new int[products.Length];

            for (int i = 0; i < products.Length; i++)
            {
                productNames[i] = products[i].Name;
                productIds[i] = products[i].ProductID;
            }

            int currentProductIndex = Mathf.Clamp(System.Array.IndexOf(productIds, selectedMission.targetId), 0, productNames.Length - 1);
            int newIndex = EditorGUILayout.Popup("Target Product", currentProductIndex, productNames);
            if (newIndex != currentProductIndex)
            {
                selectedMission.targetId = productIds[newIndex];
            }
        }

        private void ShowFurnitureDropdown()
        {
            Furniture[] furnitures = Resources.LoadAll<Furniture>("Furnitures");
            string[] furnitureNames = new string[furnitures.Length];
            int[] furnitureIds = new int[furnitures.Length];

            for (int i = 0; i < furnitures.Length; i++)
            {
                furnitureNames[i] = furnitures[i].Name;
                furnitureIds[i] = furnitures[i].FurnitureID;
            }

            int currentFurnitureIndex = Mathf.Clamp(System.Array.IndexOf(furnitureIds, selectedMission.targetId), 0, furnitureNames.Length - 1);
            int newIndex = EditorGUILayout.Popup("Target Furniture", currentFurnitureIndex, furnitureNames);
            if (newIndex != currentFurnitureIndex)
            {
                selectedMission.targetId = furnitureIds[newIndex];
            }
        }

        private void ShowLicenseDropdown()
        {
            License[] licenses = Resources.LoadAll<License>("Licenses");
            string[] licenseNames = new string[licenses.Length];
            int[] licenseIds = new int[licenses.Length];

            for (int i = 0; i < licenses.Length; i++)
            {
                licenseNames[i] = licenses[i].Name;
                licenseIds[i] = licenses[i].LicenseID;
            }

            int currentLicenseIndex = Mathf.Clamp(System.Array.IndexOf(licenseIds, selectedMission.targetId), 0, licenseNames.Length - 1);
            int newIndex = EditorGUILayout.Popup("Target License", currentLicenseIndex, licenseNames);
            if (newIndex != currentLicenseIndex)
            {
                selectedMission.targetId = licenseIds[newIndex];
            }
        }

        private void AddNewMission()
        {
            Mission newMission = ScriptableObject.CreateInstance<Mission>();

            // Automatically assign the next Mission ID and name
            int nextMissionID = missions.Length > 0 ? missions.Last().missionId + 1 : 1;
            newMission.missionId = nextMissionID;
            string assetName = $"Mission{nextMissionID:D3}";

            AssetDatabase.CreateAsset(newMission, $"Assets/CheckoutFrenzy/Resources/Missions/{assetName}.asset");
            AssetDatabase.SaveAssets();
            LoadMissions(); // Reload missions after adding a new one
        }

        private void DeleteMission(Mission mission)
        {
            if (EditorUtility.DisplayDialog("Delete Mission", $"Are you sure you want to delete Mission {mission.missionId}?", "Yes", "No"))
            {
                // Get the current page before deleting
                int previousPage = currentPage;

                // Delete the mission asset
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(mission));

                // Reload missions after deletion
                LoadMissions();

                // Adjust currentPage to ensure it doesn't go out of bounds
                int totalPages = GetTotalPages();
                if (previousPage >= totalPages && totalPages > 0)
                {
                    currentPage = totalPages - 1; // Set to the last valid page
                }

                // Ensure the current page is valid
                currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
            }
        }

        private void RenumberAllMissions()
        {
            if (EditorUtility.DisplayDialog("Renumber All Missions", "Are you sure you want to renumber all missions? This will update all mission IDs and their asset filenames.", "Yes", "No"))
            {
                for (int i = 0; i < missions.Length; i++)
                {
                    Mission mission = missions[i];

                    // Update mission ID first
                    mission.missionId = i + 1;
                    EditorUtility.SetDirty(mission); // Mark mission as dirty to trigger editor update

                    // Now check for GUID and potentially rename
                    if (missionToGUIDMap.TryGetValue(mission, out string guid))
                    {
                        string currentAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                        string newAssetPath = $"Assets/CheckoutFrenzy/Resources/Missions/Mission{mission.missionId:D3}.asset";

                        if (!currentAssetPath.Equals(newAssetPath, System.StringComparison.OrdinalIgnoreCase))
                        {
                            AssetDatabase.RenameAsset(currentAssetPath, $"Mission{mission.missionId:D3}");
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                LoadMissions();
            }
        }

        private void ShiftMission(int index, int direction)
        {
            int targetIndex = index + direction;

            if (targetIndex < 0 || targetIndex >= missions.Length) return; // Prevent out-of-bounds swap

            // Swap Missions
            Mission temp = missions[index];
            missions[index] = missions[targetIndex];
            missions[targetIndex] = temp;

            // Swap their IDs
            int tempId = missions[index].missionId;
            missions[index].missionId = missions[targetIndex].missionId;
            missions[targetIndex].missionId = tempId;

            // Mark them dirty to save changes
            EditorUtility.SetDirty(missions[index]);
            EditorUtility.SetDirty(missions[targetIndex]);

            // Save and refresh
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
