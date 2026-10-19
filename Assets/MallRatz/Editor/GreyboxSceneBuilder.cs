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
    // spill, a cart, a can pyramid, a gumball machine, a fragile vase, customer service, places to hide
    // the card, a capsule toy machine, and a player prefab that spawns once per person. Item and config
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
        static readonly Color SneakerShop = new(0.85f, 0.85f, 0.8f);
        static readonly Color ToyShop = new(0.75f, 1f, 0.7f);
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
            // Wish-list items added after the first config was made. Each is added to an existing config
            // only the run its asset is created, so taking one off by hand sticks.
            var laterWishes = new List<ItemDefinition>();
            ItemDefinition LaterWish(string name, int price, int uselessness, float carryWeight, bool bulky = false)
            {
                bool isNew = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemPath(name)) == null;
                var item = Item(name, price, uselessness, carryWeight, bulky);
                if (isNew) laterWishes.Add(item);
                return item;
            }
            var vase = LaterWish("Fancy Vase", 90, 6, 4f, bulky: true);
            var crocs = LaterWish("Limited-Edition Crocs", 60, 9, 0.8f);
            var capybara = LaterWish("Giant Plush Capybara", 70, 9, 3f, bulky: true);
            var shoeCleaner = Item("Shoe Cleaning Kit", 15, 2, 0.4f);
            var spinner = Item("Fidget Spinner", 5, 6, 0.1f);
            var config = Config(new List<ItemDefinition> { shrimp, booster, fish, sneakers, vase, crocs, capybara });
            foreach (var wish in laterWishes)
            {
                if (config.wishList.Contains(wish)) continue;
                config.wishList.Add(wish);
                EditorUtility.SetDirty(config);
            }
            var playerPrefab = PlayerPrefab();

            // Concourse runs along X between z = -12 and z = 2, with two shops north of it and two south.
            Block("Floor", new Vector3(0f, -0.5f, -5f), new Vector3(40f, 1f, 34f), Floor);
            Block("Wall N", new Vector3(0f, 1.5f, 12f), new Vector3(40f, 3f, 0.3f), WallColor);
            Block("Wall S", new Vector3(0f, 1.5f, -22f), new Vector3(40f, 3f, 0.3f), WallColor);
            Block("Wall W", new Vector3(-20f, 1.5f, -5f), new Vector3(0.3f, 3f, 34f), WallColor);
            Block("Wall E", new Vector3(20f, 1.5f, -5f), new Vector3(0.3f, 3f, 34f), WallColor);
            Shop("Junk Emporium", -11.5f, north: true, JunkShop, "mini inflatable shrimp", new Color(1f, 0.5f, 0.6f));
            Shop("Card Shop", 11.5f, north: true, CardShop, "holo card keychain", new Color(0.6f, 0.95f, 1f));
            Shop("Sneaker Store", -11.5f, north: false, SneakerShop, "sneaker keychain", new Color(1f, 0.55f, 0.1f));
            Shop("Toy Store", 11.5f, north: false, ToyShop, "mini plush", new Color(0.75f, 0.55f, 1f));

            // Junk Emporium: two shelves of wish-list junk and one expensive decoy.
            Block("Shelf A1", new Vector3(-16f, 0.5f, 6f), new Vector3(1f, 1f, 4f), Fixture);
            Block("Shelf A2", new Vector3(-7f, 0.5f, 6f), new Vector3(1f, 1f, 4f), Fixture);
            Product(shrimp, PrimitiveType.Capsule, new Vector3(-16f, 1.35f, 4.8f), new Vector3(0.6f, 0.4f, 0.6f), new Color(1f, 0.5f, 0.6f))
                .transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Product(fish, PrimitiveType.Cube, new Vector3(-16f, 1.2f, 7f), new Vector3(0.6f, 0.4f, 0.1f), new Color(0.3f, 0.6f, 0.4f));
            Product(blender, PrimitiveType.Cylinder, new Vector3(-7f, 1.25f, 5f), new Vector3(0.3f, 0.25f, 0.3f), new Color(0.8f, 0.8f, 0.85f));
            RegisterAt(new Vector3(-11.5f, 0f, 10.5f));
            VaseOnPedestal(vase, new Vector3(-11.5f, 0f, 5f));

            // Card Shop: booster packs, a register, and the can pyramid by the door.
            Block("Shelf B1", new Vector3(16f, 0.5f, 7f), new Vector3(1f, 1f, 4f), Fixture);
            for (int i = 0; i < 3; i++)
                Product(booster, PrimitiveType.Cube, new Vector3(16f, 1.1f, 5.8f + i * 1.2f), new Vector3(0.25f, 0.2f, 0.05f), new Color(1f, 0.8f, 0.1f));
            RegisterAt(new Vector3(11.5f, 0f, 10.5f));
            CanPyramid(new Vector3(8f, 0f, 3.5f));

            // Sneaker Store: the hyped pairs, a decoy, and a wobbly shoebox tower by the door.
            Block("Shelf C1", new Vector3(-16f, 0.5f, -16f), new Vector3(1f, 1f, 4f), Fixture);
            Block("Shelf C2", new Vector3(-7f, 0.5f, -16f), new Vector3(1f, 1f, 4f), Fixture);
            Product(sneakers, PrimitiveType.Cube, new Vector3(-16f, 1.1f, -17f), new Vector3(0.5f, 0.2f, 0.3f), new Color(0.9f, 0.2f, 0.9f));
            Product(crocs, PrimitiveType.Cube, new Vector3(-16f, 1.1f, -15f), new Vector3(0.45f, 0.2f, 0.3f), new Color(0.3f, 0.9f, 0.4f));
            Product(shoeCleaner, PrimitiveType.Cube, new Vector3(-7f, 1.1f, -16f), new Vector3(0.3f, 0.2f, 0.2f), new Color(0.95f, 0.95f, 0.95f));
            RegisterAt(new Vector3(-11.5f, 0f, -20.5f), facing: 1f);
            ShoeboxTower(new Vector3(-9f, 0f, -14f));

            // Toy Store: a plush too big to run with, a decoy, and a bin of bouncy balls by the door.
            Block("Shelf D1", new Vector3(16f, 0.5f, -16f), new Vector3(1f, 1f, 4f), Fixture);
            Block("Shelf D2", new Vector3(7f, 0.5f, -16f), new Vector3(1f, 1f, 4f), Fixture);
            Product(capybara, PrimitiveType.Capsule, new Vector3(16f, 1.4f, -16f), new Vector3(0.7f, 0.45f, 0.7f), new Color(0.65f, 0.45f, 0.3f))
                .transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Product(spinner, PrimitiveType.Cylinder, new Vector3(7f, 1.03f, -16f), new Vector3(0.15f, 0.03f, 0.15f), new Color(0.2f, 0.6f, 1f));
            RegisterAt(new Vector3(11.5f, 0f, -20.5f), facing: 1f);
            BouncyBallBin(new Vector3(9f, 0f, -14f));

            // Concourse traps.
            SmoothieSpill(new Vector3(0f, 0f, -3f));
            Cart(new Vector3(-6f, 0f, -6f));
            GumballMachineAt(new Vector3(4.5f, 0f, -1f));

            // Gacha machine: capsule toys that bait a Friend's weakness.
            CapsuleMachineAt(new Vector3(-5f, 0f, -0.5f));

            // Customer service, in the gap between the south shops, where Returns refunds what the Spender dropped.
            ReturnsDeskAt(new Vector3(0f, 0f, -14f));

            // Places to hide the card: three in the concourse, two in each north shop, one in each south shop.
            var bin = new Color(0.35f, 0.4f, 0.35f);
            Stash("Trash Can", PrimitiveType.Cylinder, new Vector3(15f, 0f, -4f), new Vector3(0.6f, 0.5f, 0.6f), bin);
            Stash("Potted Plant", PrimitiveType.Cylinder, new Vector3(-16f, 0f, -2f), new Vector3(0.8f, 0.4f, 0.8f), new Color(0.3f, 0.6f, 0.3f));
            Stash("Lockers", PrimitiveType.Cube, new Vector3(-17f, 0f, -11.4f), new Vector3(2f, 2f, 0.6f), new Color(0.5f, 0.55f, 0.65f));
            Stash("Bargain Bin", PrimitiveType.Cube, new Vector3(-18.5f, 0f, 10.5f), new Vector3(1.2f, 0.8f, 1.2f), JunkShop * 0.8f);
            Stash("Toy Bin", PrimitiveType.Cube, new Vector3(-4.5f, 0f, 10.5f), new Vector3(1.2f, 0.7f, 1.2f), new Color(0.9f, 0.4f, 0.2f));
            Stash("Display Case", PrimitiveType.Cube, new Vector3(17.5f, 0f, 10.8f), new Vector3(1.6f, 1.2f, 0.6f), CardShop * 0.8f);
            Stash("Box of Bulk Commons", PrimitiveType.Cube, new Vector3(18.5f, 0f, 3.5f), new Vector3(0.8f, 0.6f, 0.8f), new Color(0.75f, 0.6f, 0.4f));
            Stash("Try-on Bench", PrimitiveType.Cube, new Vector3(-18f, 0f, -20.6f), new Vector3(2f, 0.5f, 0.6f), new Color(0.55f, 0.35f, 0.25f));
            Stash("Ball Pit", PrimitiveType.Cube, new Vector3(17.5f, 0f, -20.2f), new Vector3(2f, 0.6f, 1.6f), new Color(0.95f, 0.85f, 0.3f));

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
            // Pocket cash, for if they give in. Willpower fills it.
            Set(go.GetComponent<Wallet>(), "startingBalance", 0);
            var hands = go.GetComponent<Hands>();
            Set(hands, "holdAnchor", Anchor(go.transform, "Hold Anchor", new Vector3(0f, 0.2f, 0.8f)));
            Set(hands, "cardAnchor", Anchor(go.transform, "Card Anchor", new Vector3(0.45f, 0f, 0.2f)));
            Set(go.AddComponent<Carrier>(), "carryAnchor", Anchor(go.transform, "Carry Anchor", new Vector3(0f, 1.3f, 0.9f)));
            go.AddComponent<Pickpocket>();
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

        // A 17 x 10 shop on the north (z 2 to 12) or south (z -22 to -12) side of the concourse.
        static void Shop(string name, float centerX, bool north, Color color, string trinketName, Color trinketColor)
        {
            const float width = 17f, door = 4f;
            float frontZ = north ? 2f : -12f;
            float centerZ = north ? 7f : -17f;
            Block(name + " Floor", new Vector3(centerX, 0.01f, centerZ), new Vector3(width, 0.02f, 10f), color).GetComponent<Collider>().enabled = false;

            // Front wall facing the concourse, with a door in the middle.
            float side = (width - door) / 2f;
            Block(name + " Front L", new Vector3(centerX - (door + side) / 2f, 1.5f, frontZ), new Vector3(side, 3f, 0.3f), WallColor);
            Block(name + " Front R", new Vector3(centerX + (door + side) / 2f, 1.5f, frontZ), new Vector3(side, 3f, 0.3f), WallColor);
            float inner = centerX + Mathf.Sign(centerX) * -width / 2f;
            Block(name + " Side", new Vector3(inner, 1.5f, centerZ), new Vector3(0.3f, 3f, 10f), WallColor);

            var zone = new GameObject(name);
            zone.transform.position = new Vector3(centerX, 1.5f, centerZ);
            zone.AddComponent<BoxCollider>().size = new Vector3(width, 3f, 10f);
            var shop = zone.AddComponent<ShopZone>();
            Set(shop, "shopName", name);
            Set(shop, "trinketName", trinketName);
            Set(shop, "trinketColor", trinketColor);
        }

        // Facing -1 puts the queue south of the counter (north shops), +1 north of it (south shops).
        static void RegisterAt(Vector3 position, float facing = -1f)
        {
            Block("Register Counter", position + new Vector3(0f, 0.5f, 0f), new Vector3(2f, 1f, 0.8f), Fixture);
            var register = new GameObject("Register");
            register.transform.position = position + new Vector3(0f, 1f, 1.4f * facing);
            register.AddComponent<BoxCollider>().size = new Vector3(3f, 2f, 2f);
            register.AddComponent<NetworkObject>();
            register.AddComponent<Register>();
        }

        // The capsules are their own props, waiting out of play inside the cabinet until someone pulls.
        static void CapsuleMachineAt(Vector3 position)
        {
            var cabinet = Block("Capsule Machine", position + new Vector3(0f, 0.8f, 0f), new Vector3(0.8f, 1.6f, 0.6f), new Color(0.95f, 0.45f, 0.7f));
            var capsules = new Object[6];
            for (int i = 0; i < capsules.Length; i++)
            {
                var capsule = Prim($"Capsule {i + 1}", PrimitiveType.Sphere, cabinet.transform.position, Vector3.one * 0.15f, Color.white);
                capsule.AddComponent<Rigidbody>().mass = 0.1f;
                Networked(capsule);
                capsules[i] = capsule.AddComponent<Trinket>();
            }

            var machine = new GameObject("Capsule Toys");
            machine.transform.position = position + Vector3.up;
            machine.AddComponent<BoxCollider>().size = new Vector3(2.5f, 2f, 2.5f);
            machine.AddComponent<NetworkObject>();
            SetArray(machine.AddComponent<CapsuleMachine>(), "capsules", capsules);
        }

        static void ReturnsDeskAt(Vector3 position)
        {
            Block("Customer Service Counter", position + new Vector3(0f, 0.5f, 0f), new Vector3(3f, 1f, 0.8f), new Color(0.3f, 0.5f, 0.8f));
            var desk = new GameObject("Customer Service");
            desk.transform.position = position + new Vector3(0f, 1f, 1.4f);
            desk.AddComponent<BoxCollider>().size = new Vector3(4f, 2f, 2f);
            desk.AddComponent<NetworkObject>();
            desk.AddComponent<ReturnsDesk>();
        }

        // A solid thing to hide the card in, with a zone around it to stand in.
        static void Stash(string name, PrimitiveType shape, Vector3 floor, Vector3 scale, Color color)
        {
            // Primitive cylinders are 2 units tall, cubes 1.
            float height = shape == PrimitiveType.Cylinder ? scale.y * 2f : scale.y;
            Prim(name, shape, floor + Vector3.up * (height / 2f), scale, color);
            var stash = new GameObject(name + " Stash");
            stash.transform.position = floor + Vector3.up;
            stash.AddComponent<BoxCollider>().size = new Vector3(scale.x + 2f, 2f, scale.z + 2f);
            stash.AddComponent<NetworkObject>();
            Set(stash.AddComponent<CardStash>(), "label", name);
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

        // A top-heavy rigidbody made of several colliders, with a glass globe that breaks into loose
        // gumballs. The gumballs are their own props, parked out of sight until it breaks.
        static void GumballMachineAt(Vector3 position)
        {
            var red = new Color(0.85f, 0.15f, 0.15f);
            var machine = new GameObject("Gumball Machine");
            machine.transform.position = position;
            Part(machine, "Base", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(0.7f, 0.1f, 0.7f), red);
            Part(machine, "Stem", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(0.15f, 0.8f, 0.15f), new Color(0.8f, 0.8f, 0.85f));
            Part(machine, "Head", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), new Vector3(0.4f, 0.3f, 0.4f), red);
            var globe = Part(machine, "Globe", PrimitiveType.Sphere, new Vector3(0f, 1.47f, 0f), Vector3.one * 0.55f, new Color(0.85f, 0.95f, 1f));
            machine.AddComponent<Rigidbody>().mass = 35f;
            Networked(machine);
            Set(machine.AddComponent<TripOnImpact>(), "minImpactSpeed", 2f);
            Set(machine.AddComponent<Resettable>(), "resetDelay", 30f);

            var trap = machine.AddComponent<GumballMachine>();
            Set(trap, "globe", globe.GetComponent<SphereCollider>());
            SetArray(trap, "gumballs", Balls("Gumball", 20, globe.transform.position, 0.12f, 0.05f));
        }

        // The same trap as a toy-store bin: a heap of bouncy balls on top that spills when it tips or
        // takes a hard hit. Fewer, bigger balls than the gumball machine.
        static void BouncyBallBin(Vector3 position)
        {
            var bin = new GameObject("Bouncy Ball Bin");
            bin.transform.position = position;
            Part(bin, "Bin", PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(0.7f, 0.4f, 0.7f), new Color(0.3f, 0.6f, 0.95f));
            var heap = Part(bin, "Heap", PrimitiveType.Sphere, new Vector3(0f, 0.85f, 0f), Vector3.one * 0.6f, new Color(1f, 0.4f, 0.3f));
            bin.AddComponent<Rigidbody>().mass = 15f;
            Networked(bin);
            Set(bin.AddComponent<Resettable>(), "resetDelay", 30f);

            var trap = bin.AddComponent<GumballMachine>();
            Set(trap, "globe", heap.GetComponent<SphereCollider>());
            Set(trap, "centerOfMass", new Vector3(0f, 0.6f, 0f));
            SetArray(trap, "gumballs", Balls("Bouncy Ball", 12, heap.transform.position, 0.2f, 0.08f));
        }

        static Object[] Balls(string name, int count, Vector3 position, float size, float mass)
        {
            Color[] flavours = { new(1f, 0.3f, 0.3f), new(1f, 0.85f, 0.2f), new(0.3f, 0.8f, 0.3f), new(0.3f, 0.5f, 1f), new(1f, 0.5f, 0.85f) };
            var balls = new Object[count];
            for (int i = 0; i < count; i++)
            {
                var ball = Prim($"{name} {i + 1}", PrimitiveType.Sphere, position, Vector3.one * size, flavours[i % flavours.Length]);
                var body = ball.AddComponent<Rigidbody>();
                body.mass = mass;
                body.linearDamping = 0.2f;
                body.angularDamping = 0.8f;
                ball.GetComponent<Collider>().sharedMaterial = Bouncy();
                Networked(ball);
                balls[i] = ball.AddComponent<Gumball>();
            }
            return balls;
        }

        // A collapsing display for the sneaker store: shoeboxes stacked in a slightly crooked column.
        static void ShoeboxTower(Vector3 basePosition)
        {
            var box = new Vector3(0.45f, 0.22f, 0.3f);
            for (int i = 0; i < 6; i++)
            {
                var offset = new Vector3((i % 2 == 0 ? 1f : -1f) * 0.04f, box.y * (i + 0.5f), 0f);
                var shoebox = Prim("Shoebox", PrimitiveType.Cube, basePosition + offset, box, i % 2 == 0 ? new Color(0.9f, 0.5f, 0.2f) : new Color(0.25f, 0.25f, 0.3f));
                shoebox.transform.rotation = Quaternion.Euler(0f, i * 7f, 0f);
                shoebox.AddComponent<Rigidbody>().mass = 0.6f;
                Networked(shoebox);
                Set(shoebox.AddComponent<TripOnImpact>(), "minImpactSpeed", 2f);
                Set(shoebox.AddComponent<Resettable>(), "resetDelay", 25f);
            }
        }

        static GameObject Part(GameObject parent, string name, PrimitiveType shape, Vector3 localPosition, Vector3 scale, Color color)
        {
            var part = Prim(name, shape, Vector3.zero, scale, color);
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = localPosition;
            return part;
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

        // Heavy and fragile, R.E.P.O.-style: a round belly on a flat foot, with a neck, on its own plinth.
        static void VaseOnPedestal(ItemDefinition definition, Vector3 floor)
        {
            var porcelain = new Color(0.25f, 0.45f, 0.85f);
            Block("Vase Pedestal", floor + new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.6f), Fixture);
            var vase = Prim(definition.displayName, PrimitiveType.Sphere, floor + new Vector3(0f, 1.155f, 0f), new Vector3(0.45f, 0.5f, 0.45f), porcelain);
            // Without a flat foot the belly would roll off the plinth at the slightest touch.
            Part(vase, "Foot", PrimitiveType.Cylinder, new Vector3(0f, -0.45f, 0f), new Vector3(0.6f, 0.06f, 0.6f), porcelain);
            Part(vase, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.4f, 0.5f), porcelain);
            vase.AddComponent<Rigidbody>().mass = definition.carryWeight;
            Networked(vase);
            Set(vase.AddComponent<FragileItem>(), "definition", definition);
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

        static PhysicsMaterial Bouncy()
        {
            string path = Root + "/Art/Greybox/Bouncy.asset";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material != null) return material;
            EnsureFolder(Root + "/Art/Greybox");
            material = new PhysicsMaterial("Bouncy")
            {
                bounciness = 0.6f,
                staticFriction = 0.4f,
                dynamicFriction = 0.3f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static string ItemPath(string name) => $"{Root}/Data/Items/{name}.asset";

        static ItemDefinition Item(string name, int price, int uselessness, float carryWeight, bool bulky = false)
        {
            string path = ItemPath(name);
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null) return item;
            EnsureFolder(Root + "/Data/Items");
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.displayName = name;
            item.price = price;
            item.uselessness = uselessness;
            item.carryWeight = carryWeight;
            item.bulky = bulky;
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
                case int number: property.intValue = number; break;
                case Vector3 vector: property.vector3Value = vector; break;
                case Color color: property.colorValue = color; break;
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
