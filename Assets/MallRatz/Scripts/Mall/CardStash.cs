using System.Collections.Generic;
using MallRatz.Items;
using MallRatz.Players;
using MallRatz.Round;
using MallRatz.UI;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Mall
{
    // A locker, a trash can, a bargain bin: somewhere the Friends can hide the card instead of holding
    // it. That frees their hands but leaves it unguarded. Friends know where it is and put it in or take
    // it out at once; the Spender has to rummage through each stash, and everyone can see her doing it.
    // Host only; everyone sees the progress.
    [RequireComponent(typeof(BoxCollider))]
    public class CardStash : NetworkBehaviour
    {
        static readonly List<CardStash> all = new();

        [SerializeField] string label = "Stash";
        [SerializeField, Min(0)] float searchSeconds = 2f;
        [SerializeField, Min(0)] float labelHeight = 1.6f;

        readonly NetworkVariable<float> progress = new();

        BoxCollider zone;
        MallRatzPlayer searcher;
        float resultShownAt = float.NegativeInfinity;
        bool resultFound;

        public string Label => label;

        bool HasCard
        {
            get
            {
                var card = RoundManager.Instance != null ? RoundManager.Instance.Card : null;
                return card != null && card.StashedIn == this;
            }
        }

        void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public static CardStash At(Vector3 position)
        {
            foreach (var stash in all)
                if (stash.zone.bounds.Contains(position)) return stash;
            return null;
        }

        // E at a stash. True if it did something, so the press isn't also a pick-up.
        public bool ServerTryUse(MallRatzPlayer player)
        {
            if (player.Body.State != BodyState.Upright) return false;
            var card = RoundManager.Instance.Card;

            if (player.Role == PlayerRole.Friend)
            {
                if (player.Hands.Card != null)
                {
                    card.ServerStash(this);
                    return true;
                }
                if (!HasCard) return false;
                card.ServerUnstash();
                player.Hands.ServerTakeCard(card);
                return true;
            }

            if (searcher != null || player.Hands.Card != null) return false;
            searcher = player;
            progress.Value = 0.01f;
            return true;
        }

        void Update()
        {
            if (!IsServer || searcher == null) return;
            if (searcher.Body.State != BodyState.Upright || !zone.bounds.Contains(searcher.transform.position))
            {
                Stop();
                return;
            }

            progress.Value = searchSeconds > 0f ? Mathf.Min(1f, progress.Value + Time.deltaTime / searchSeconds) : 1f;
            if (progress.Value < 1f) return;

            bool found = HasCard;
            if (found)
            {
                var card = RoundManager.Instance.Card;
                card.ServerUnstash();
                searcher.Hands.ServerTakeCard(card);
            }
            ResultRpc(found);
            Stop();
        }

        void Stop()
        {
            searcher = null;
            progress.Value = 0f;
        }

        [Rpc(SendTo.Everyone)]
        void ResultRpc(bool found)
        {
            resultShownAt = Time.time;
            resultFound = found;
        }

        // Everyone sees the stash's name and any rummaging. Friends also see which one holds the card.
        void OnGUI()
        {
            if (!IsSpawned) return;
            var me = MallRatzPlayer.Local;
            var top = transform.position + Vector3.up * labelHeight;

            if (Time.time - resultShownAt < 2f)
                WorldLabel.Draw(top, resultFound ? "She found the card!" : "Nothing here", resultFound ? Color.red : Color.white);
            else if (progress.Value > 0f)
                WorldLabel.Draw(top, $"Rummaging through the {label.ToLower()}... {progress.Value:P0}", Color.yellow);
            else if (me != null && me.Role == PlayerRole.Friend && HasCard)
                WorldLabel.Draw(top, $"{label}\n(card hidden here)", Color.green);
            else if (me != null && (me.transform.position - transform.position).sqrMagnitude < 36f)
                WorldLabel.Draw(top, label, new Color(1f, 1f, 1f, 0.7f));
        }
    }
}
