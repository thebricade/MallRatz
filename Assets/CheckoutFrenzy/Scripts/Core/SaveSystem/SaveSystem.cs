using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public static class SaveSystem
    {
        /// <summary>
        /// Saves data to a file with a specified name using JSON serialization.
        /// </summary>
        /// <typeparam name="T">The type of the data to be saved.</typeparam>
        /// <param name="data">The data to be saved.</param>
        /// <param name="fileName">The name of the file to save the data to.</param>
        public static void SaveData<T>(T data, string fileName)
        {
            // Construct the full path to the save file
            string filePath = Application.persistentDataPath + "/" + fileName + ".json";

            // Serialize the data to a JSON string
            // Formatting.Indented makes the save file human-readable
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            // Write the JSON string to the file
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads data from a JSON file with a specified name.
        /// </summary>
        /// <typeparam name="T">The type of the data to be loaded.</typeparam>
        /// <param name="fileName">The name of the file to load the data from.</param>
        /// <returns>The loaded data, or default(T) if the file does not exist.</returns>
        public static T LoadData<T>(string fileName)
        {
            // Construct the full path to the save file
            string filePath = Application.persistentDataPath + "/" + fileName + ".json";

            // Check if the file exists before attempting to load it
            if (File.Exists(filePath))
            {
                // Read the JSON string from the file
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON string back into the data object
                T data = JsonConvert.DeserializeObject<T>(json);

                return data;
            }
            else
            {
                // Optional: Log an error message if the file does not exist
                // Debug.LogWarning("Save file not found in " + filePath);

                // Return default value if the file does not exist
                return default(T);
            }
        }
    }
}
