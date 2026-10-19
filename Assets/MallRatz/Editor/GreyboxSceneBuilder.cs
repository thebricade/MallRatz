using System.Collections.Generic;
using System.Reflection;
using MallRatz.Items;
using MallRatz.Mall;
using MallRatz.Net;
using MallRatz.Players;
using MallRatz.Round;
using MallRatz.Traps;
using MallRatz.UI;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MallRatz.Editor
{
    // Builds the roadmap's first scene, networked: a concourse, two shops with registers, a smoothie
    // spill, a cart, a can pyramid, and a player prefab that spawns once per person. Item and config
    // assets are only created if missing, so tuning them by hand survives a rebuild; the scene and the
    // player prefab are replaced.
    public static class GreyboxSceneBuilder
    {
        const string Root = "Assets/MallRatz";
        const string ScenePath = Root + "/Scenes/Greybox.unity";
        const string PlayerPrefabPath = Root + "/Prefabs/Player.prefab";
        const string NetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";

        static readonly Color Floor = new(0.75f, 0.75f, 0.72f);
        static readonly Color WallColor = new(0.9f, 0.88f, 0.84f);
        static readonly Color JunkShop = new(1f, 0.85f, 0.55f);
        static readonly Color CardShop = new(0.6f, 0.8f, 1f);
        static readonly Color Fixture = new(0.45f, 0.4f, 0.38f);

        [MenuItem("MallRatz/Build Greybox Scene")]
        public static void Build()
        {
            if (!Application.isBatchMode)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                    && !EditorUtility.DisplayDialog("Rebuild greybox?", $"This replaces {ScenePath} and the player prefab.", "Rebuild", "Cancel"))
                    return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            }

            // New scene first: opening one unloads assets nothing references yet, which would null these out.
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var shrimp = Item("Giant Inflatable Shrimp", 40, 10, 2f);
            var blender = Item("Blender", 200, 2, 1.5f);
            var booster = Item("Booster Pack", 5, 6, 0.1f);
            var fish = Item("Singing Fish Plaque", 25, 9, 0.8f);
            var sneakers = Item("Light-up Sneakers", 80, 7, 0.6f);
            var config = Config(new List<ItemDefinition> { shrimp, booster, fish, sneakers });
            var playerPrefab = PlayerPrefab();

            // Concourse runs along X at z < 2; the two shops sit north of it.
            Block("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 24f), Floor);
            Block("Wall N", new Vector3(0f, 1.5f, 12f), new Vector3(40f, 3f, 0.3f), WallColor);
            Block("Wall S", new Vector3(0f, 1.5f, -12f), new Vector3(40f, 3f, 0.3f), WallColor);
            Block("Wall W", new Vector3(-20f, 1.5f, 0f), new Vector3(0.3f, 3f, 24f), WallColor);
            Block("Wall E", new Vector3(20f, 1.5f, 0f), new Vector3(0.3f, 3f, 24f), WallColor);
            Shop("Junk Emporium", -11.5f, JunkShop);
            Shop("Card Shop", 11.5f, CardShop);

            // Junk Emporium: two shelves of wish-list junk and one expensive decoy.
            Block("Shelf A1", new Vector3(-16f, 0.5f, 6f), new Vector3(1f, 1f, 4f), Fixture);
            Block("Shelf A2", new Vector3(-7f, 0.5f, 6f), new Vector3(1f, 1f, 4f), Fixture);
            Product(shrimp, PrimitiveType.Capsule, new Vector3(-16f, 1.35f, 4.8f), new Vector3(0.6f, 0.4f, 0.6f), new Color(1f, 0.5f, 0.6f))
                .transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Product(fish, PrimitiveType.Cube, new Vector3(-16f, 1.2f, 7f), new Vector3(0.6f, 0.4f, 0.1f), new Color(0.3f, 0.6f, 0.4f));
            Product(blender, PrimitiveType.Cylinder, new Vector3(-7f, 1.25f, 5f), new Vector3(0.3f, 0.25f, 0.3f), new Color(0.8f, 0.8f, 0.85f));
            Product(sneakers, PrimitiveType.Cube, new Vector3(-7f, 1.1f, 7f), new Vector3(0.5f, 0.2f, 0.3f), new Color(0.9f, 0.2f, 0.9f));
            RegisterAt(new Vector3(-11.5f, 0f, 10.5f));

            // Card Shop: booster packs, a register, and the can pyramid by the door.
            Block("Shelf B1", new Vector3(16f, 0.5f, 7f), new Vector3(1f, 1f, 4f), Fixture);
            for (int i = 0; i < 3; i++)
                Product(booster, PrimitiveType.Cube, new Vector3(16f, 1.1f, 5.8f + i * 1.2f), new Vector3(0.25f, 0.2f, 0.05f), new Color(1f, 0.8f, 0.1f));
            RegisterAt(new Vector3(11.5f, 0f, 10.5f));
            CanPyramid(new Vector3(8f, 0f, 3.5f));

            // Concourse traps.
            SmoothieSpill(new Vector3(0f, 0f, -3f));
            Cart(new Vector3(-6f, 0f, -6f));

            // Round: the clock, the Spender's wallet and card, and where people spawn.
            var roundObject = new GameObject("Round Manager");
            roundObject.AddComponent<NetworkObject>();
            var round = roundObject.AddComponent<RoundManager>();
            var wallet = roundObject.AddComponent<Wallet>();
            var card = Prim("Card", PrimitiveType.Cube, new Vector3(0f, 0.05f, -8f), new Vector3(0.2f, 0.02f, 0.12f), new Color(0.2f, 0.9f, 0.4f));
            card.AddComponent<Rigidbody>().mass = 0.1f;
            Networked(card);
            Set(card.AddComponent<PaymentCard>(), "wallet", wallet);
            var spawns = new List<Transform>();
            for (int i = 0; i < 6; i++)
            {
                var spawn = new GameObject($"Spawn {i + 1}").transform;
                spawn.position = new Vector3(-5f + i * 2f, 1.1f, -8f);
                spawns.Add(spawn);
            }
            Set(round, "config", config);
            Set(round, "card", card.GetComponent<PaymentCard>());
            Set(round, "wallet", wallet);
            SetArray(round, "spawnPoints", spawns.ToArray());

            // Networking, input, camera, HUD.
            var networkObject = new GameObject("Network Manager");
            var network = networkObject.AddComponent<NetworkManager>();
            var transport = networkObject.AddComponent<UnityTransport>();
            network.NetworkConfig.NetworkTransport = transport;
            network.NetworkConfig.PlayerPrefab = playerPrefab;
            network.NetworkConfig.Prefabs.NetworkPrefabsLists = new List<NetworkPrefabsList> { PrefabList(playerPrefab) };
            EditorUtility.SetDirty(network);

            var camera = Camera.main;
            var ui = new GameObject("Local Input and HUD");
            var input = ui.AddComponent<LocalPlayerInput>();
            Set(input, "cameraTransform", camera.transform);
            Set(ui.AddComponent<DebugHud>(), "input", input);
            ui.AddComponent<SessionMenu>();
            ui.AddComponent<RoundBanner>();
            camera.transform.SetPositionAndRotation(new Vector3(0f, 8f, -16f), Quaternion.Euler(40f, 0f, 0f));
            Set(camera.gameObject.AddComponent<FollowCamera>(), "input", input);

            // In-scene network objects only get their IDs once the scene is saved and in the build list,
            // so save, add it, then refresh the IDs and save again.
            EnsureFolder(Root + "/Scenes");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var validate = typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var networked in Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include))
            {
                validate?.Invoke(networked, null);
                EditorUtility.SetDirty(networked);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"Built {ScenePath}. Press Play, then Host local (or Host online for a join code).");
        }

        // Everything a person controls. Spawned once per connection by the NetworkManager.
        static GameObject PlayerPrefab()
        {
            var go = Prim("Player", PrimitiveType.Capsule, Vector3.zero, Vector3.one, new Color(0.95f, 0.55f, 0.3f));
            var nose = Prim("Nose", PrimitiveType.Cube, Vector3.zero, new Vector3(0.3f, 0.2f, 0.3f), Color.white);
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(go.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.5f, 0.45f);

            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            go.AddComponent<CharacterController>();
            go.AddComponent<Rigidbody>().isKinematic = true;
            go.AddComponent<MallRatzPlayer>();
            var hands = go.GetComponent<Hands>();
            Set(hands, "holdAnchor", Anchor(go.transform, "Hold Anchor", new Vector3(0f, 0.2f, 0.8f)));
            Set(hands, "cardAnchor", Anchor(go.transform, "Card Anchor", new Vector3(0.45f, 0f, 0.2f)));
            Set(go.AddComponent<Carrier>(), "carryAnchor", Anchor(go.transform, "Carry Anchor", new Vector3(0f, 1.3f, 0.9f)));
            go.AddComponent<PlaceholderMover>();
            go.AddComponent<PlayerVisuals>();

            EnsureFolder(Root + "/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static NetworkPrefabsList PrefabList(GameObject playerPrefab)
        {
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
            if (list == null)
            {
                list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                AssetDatabase.CreateAsset(list, NetworkPrefabsPath);
            }
            if (!list.Contains(playerPrefab))
            {
                list.Add(new NetworkPrefab { Prefab = playerPrefab });
                EditorUtility.SetDirty(list);
                AssetDatabase.SaveAssets();
            }
            return list;
        }

        static void Shop(string name, float centerX, Color color)
        {
            const float width = 17f, door = 4f;
            Block(name + " Floor", new Vector3(centerX, 0.01f, 7f), new Vector3(width, 0.02f, 10f), color).GetComponent<Collider>().enabled = false;

            // Front wall at z = 2 with a door in the middle.
            float side = (width - door) / 2f;
            Block(name + " Front L", new Vector3(centerX - (door + side) / 2f, 1.5f, 2f), new Vector3(side, 3f, 0.3f), WallColor);
            Block(name + " Front R", new Vector3(centerX + (door + side) / 2f, 1.5f, 2f), new Vector3(side, 3f, 0.3f), WallColor);
            float inner = centerX + Mathf.Sign(centerX) * -width / 2f;
            Block(name + " Side", new Vector3(inner, 1.5f, 7f), new Vector3(0.3f, 3f, 10f), WallColor);

            var zone = new GameObject(name);
            zone.transform.position = new Vector3(centerX, 1.5f, 7f);
            zone.AddComponent<BoxCollider>().size = new Vector3(width, 3f, 10f);
            Set(zone.AddComponent<ShopZone>(), "shopName", name);
        }

        static void RegisterAt(Vector3 position)
        {
            Block("Register Counter", position + new Vector3(0f, 0.5f, 0f), new Vector3(2f, 1f, 0.8f), Fixture);
            var register = new GameObject("Register");
            register.transform.position = position + new Vector3(0f, 1f, -1.4f);
            register.AddComponent<BoxCollider>().size = new Vector3(3f, 2f, 2f);
            register.AddComponent<NetworkObject>();
            register.AddComponent<Register>();
        }

        static void SmoothieSpill(Vector3 position)
        {
            Block("Smoothie Table", position + new Vector3(0f, 0.4f, 0f), new Vector3(1f, 0.8f, 1f), Fixture);
            var cup = Prim("Smoothie", PrimitiveType.Cylinder, position + new Vector3(0f, 1.05f, 0f), new Vector3(0.2f, 0.15f, 0.2f), new Color(0.95f, 0.4f, 0.5f));
            cup.AddComponent<Rigidbody>().mass = 0.3f;
            Networked(cup);
            Set(cup.AddComponent<Resettable>(), "resetDelay", 15f);

            var patch = Prim("Spill", PrimitiveType.Cube, position + new Vector3(0f, 0.01f, 0f), new Vector3(2.5f, 0.02f, 2.5f), new Color(1f, 0.55f, 0.65f));
            var trigger = patch.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1f, 50f, 1f); // 1 m tall once scaled
            patch.AddComponent<SlipperySurface>();
            Set(cup.AddComponent<SpillSource>(), "patch", patch.GetComponent<SlipperySurface>());
        }

        static void Cart(Vector3 position)
        {
            var cart = Prim("Shopping Cart", PrimitiveType.Cube, position + new Vector3(0f, 0.5f, 0f), new Vector3(0.8f, 0.8f, 1.2f), new Color(0.7f, 0.7f, 0.75f));
            var body = cart.AddComponent<Rigidbody>();
            body.mass = 15f;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            cart.GetComponent<Collider>().sharedMaterial = Slippery();
            Networked(cart);
            cart.AddComponent<TripOnImpact>();
            Set(cart.AddComponent<Resettable>(), "resetDelay", 30f);
        }

        static void CanPyramid(Vector3 basePosition)
        {
            const float size = 0.3f;
            var color = new Color(0.85f, 0.2f, 0.2f);
            for (int row = 0; row < 4; row++)
            for (int i = 0; i < 4 - row; i++)
            {
                float x = (i - (3 - row) / 2f) * size;
                var can = Prim("Can", PrimitiveType.Cylinder, basePosition + new Vector3(x, size * (row + 0.5f), 0f), new Vector3(size * 0.9f, size / 2f, size * 0.9f), color);
                can.AddComponent<Rigidbody>().mass = 0.4f;
                Networked(can);
                Set(can.AddComponent<TripOnImpact>(), "minImpactSpeed", 2f);
                Set(can.AddComponent<Resettable>(), "resetDelay", 25f);
            }
        }

        static Transform Anchor(Transform parent, string name, Vector3 localPosition)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = localPosition;
            return anchor;
        }

        static GameObject Product(ItemDefinition definition, PrimitiveType shape, Vector3 position, Vector3 scale, Color color)
        {
            var go = Prim(definition.displayName, shape, position, scale, color);
            go.AddComponent<Rigidbody>().mass = Mathf.Max(0.2f, definition.carryWeight);
            Networked(go);
            Set(go.AddComponent<ShopItem>(), "definition", definition);
            return go;
        }

        // A host-simulated physics prop.
        static void Networked(GameObject go)
        {
            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkProp>();
        }

        static GameObject Block(string name, Vector3 position, Vector3 scale, Color color) =>
            Prim(name, PrimitiveType.Cube, position, scale, color);

        static GameObject Prim(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (shape == PrimitiveType.Cylinder)
            {
                // The default capsule collider has round ends, so cans and cups would never stand still.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<MeshCollider>().convex = true;
            }
            return go;
        }

        static Material Material(Color color)
        {
            // Match whatever pipeline the project renders with; Checkout Frenzy's art uses the built-in one.
            var shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");

            string path = $"{Root}/Art/Greybox/{ColorUtility.ToHtmlStringRGB(color)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                EnsureFolder(Root + "/Art/Greybox");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        static PhysicsMaterial Slippery()
        {
            string path = Root + "/Art/Greybox/Slippery.asset";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material != null) return material;
            EnsureFolder(Root + "/Art/Greybox");
            material = new PhysicsMaterial("Slippery")
            {
                staticFriction = 0.02f,
                dynamicFriction = 0.02f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static ItemDefinition Item(string name, int price, int uselessness, float carryWeight)
        {
            string path = $"{Root}/Data/Items/{name}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null) return item;
            EnsureFolder(Root + "/Data/Items");
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.displayName = name;
            item.price = price;
            item.uselessness = uselessness;
            item.carryWeight = carryWeight;
            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        static RoundConfig Config(List<ItemDefinition> wishList)
        {
            string path = Root + "/Data/RoundConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<RoundConfig>(path);
            if (config != null) return config;
            EnsureFolder(Root + "/Data");
            config = ScriptableObject.CreateInstance<RoundConfig>();
            config.wishList = wishList;
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            switch (value)
            {
                case Object reference: property.objectReferenceValue = reference; break;
                case float number: property.floatValue = number; break;
                case string text: property.stringValue = text; break;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
