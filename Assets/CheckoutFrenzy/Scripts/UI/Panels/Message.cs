using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Message : UIPanel
    {
        private TMP_Text messageText;
        private Queue<(string message, Color color, float time)> messageQueue = new();
        private bool isDisplaying = false;

        protected override void Awake()
        {
            base.Awake();

            messageText = GetComponentInChildren<TMP_Text>();
            messageText.text = "";

            HideUI();
        }

        private void OnEnable()
        {
            UIEvents.OnMessageLogged += Log;
        }

        private void OnDisable()
        {
            UIEvents.OnMessageLogged -= Log;
        }

        /// <summary>
        /// Displays a message with an optional color and display time.
        /// Messages are queued if one is already being displayed.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="color">The color of the message text (optional, defaults to white).</param>
        /// <param name="time">The duration (in seconds) to display the message (defaults to 2 seconds).</param>
        private void Log(string message, Color? color = null, float time = 1f)
        {
            if (messageQueue.Count > 0)
            {
                var (lastMsg, _, _) = messageQueue.Peek();

                if (lastMsg == message)
                    return;
            }

            messageQueue.Enqueue((message, color ?? Color.white, time));

            if (!isDisplaying)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            isDisplaying = true;

            while (messageQueue.Count > 0)
            {
                var (message, color, time) = messageQueue.Peek();

                messageText.text = $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{message}</color>";

                DOTween.Kill(canvasGroup);
                yield return canvasGroup.DOFade(1f, 0.25f).WaitForCompletion(); // Fade in
                yield return new WaitForSeconds(time); // Display duration
                yield return canvasGroup.DOFade(0f, 0.25f).WaitForCompletion(); // Fade out

                messageQueue.Dequeue();
            }

            isDisplaying = false;
        }
    }
}
