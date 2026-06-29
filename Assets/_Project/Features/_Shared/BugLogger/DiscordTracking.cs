using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Easygoing.Features.Shared.Tracking
{
    public static class DiscordTracking
    {
        #region Fields

        private static readonly string _webhookUrl =
            "https://discord.com/api/webhooks/1486629559423209512/oJWPEyZYOxUtDMSKi-A-Wkv-FvL_1mKx1Njzc6Vw1WCkAgQT5sGMJ-ae9JVgD2l91JP3";

        private static readonly string _webhookFeedbackUrl =
            "https://discord.com/api/webhooks/1486629395778109450/v7KPtvQ1ybVsQNyl7PTACaHUi1v2IZD46qr5IjoUBftEBIAPOsHp8TCdn3oTBaq3-YJY";

        private static readonly string _botToken =
            "MTM2ODg5NjgyMDU4MzA3NTg4MA.GuBdtr.yImVPJGg0n202WvFripRRTLLDvtnGKeu4TKlwY";

        private static readonly string _targetChannelId = "1458356915930403026";

        private const int MAX_THREAD_NAME_LENGTH = 100;
        private const int MAX_MESSAGE_CONTENT_LENGTH = 2000;
        private const string DEFAULT_THREAD_NAME = "Bug Report";

        /// <summary>
        ///     Represents a Discord forum tag with its name and unique identifier.
        /// </summary>
        public struct DiscordForumTag
        {
            /// <summary>
            ///     The display name of the tag.
            /// </summary>
            public string Name;

            /// <summary>
            ///     The unique ID of the tag.
            /// </summary>
            public string Id;

            /// <summary>
            ///     Initializes a new instance of the <see cref="DiscordForumTag" /> struct.
            /// </summary>
            /// <param name="name">The display name of the tag.</param>
            /// <param name="id">The unique ID of the tag.</param>
            public DiscordForumTag(string name, string id)
            {
                Name = name;
                Id = id;
            }
        }

        private static DiscordForumTag[] _cachedTags;

        [Serializable]
        private class ChannelResponse
        {
            public TagData[] available_tags;
        }

        [Serializable]
        private class TagData
        {
            public string id;
            public string name;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Returns the cached forum tags without fetching. Returns null if not yet loaded.
        /// </summary>
        /// <returns>An array of cached <see cref="DiscordForumTag" />, or null if not yet loaded.</returns>
        public static DiscordForumTag[] GetCachedTags()
        {
            return _cachedTags;
        }

        /// <summary>
        ///     Returns forum tags for the target channel. Fetches from Discord API once per session, then returns cached result.
        /// </summary>
        /// <returns>A UniTask containing an array of available <see cref="DiscordForumTag" />.</returns>
        public static async UniTask<DiscordForumTag[]> GetAvailableTagsAsync()
        {
            if (_cachedTags != null) return _cachedTags;

            var url = $"https://discord.com/api/v10/channels/{_targetChannelId}";
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Authorization", $"Bot {_botToken}");

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DiscordTracking] Failed to fetch forum tags: {req.error}");
                _cachedTags = Array.Empty<DiscordForumTag>();
                return _cachedTags;
            }

            var channel = JsonUtility.FromJson<ChannelResponse>(req.downloadHandler.text);
            if (channel?.available_tags == null)
            {
                _cachedTags = Array.Empty<DiscordForumTag>();
                return _cachedTags;
            }

            _cachedTags = new DiscordForumTag[channel.available_tags.Length];
            for (var i = 0; i < channel.available_tags.Length; i++)
                _cachedTags[i] = new DiscordForumTag(channel.available_tags[i].name, channel.available_tags[i].id);

            return _cachedTags;
        }

        /// <summary>
        ///     Creates a new thread in the target Discord forum channel and uploads a screenshot as an attachment.
        /// </summary>
        /// <param name="threadTitle">Title of the thread to create.</param>
        /// <param name="messageContent">Body message content of the thread.</param>
        /// <param name="screenshotBytes">PNG screenshot bytes to attach.</param>
        /// <param name="selectedTagIds">Optional list of forum tag IDs to apply to the thread.</param>
        public static void TrackingCreateThreadWithScreenshot(
            string threadTitle,
            string messageContent,
            byte[] screenshotBytes,
            string[] selectedTagIds = null)
        {
            CreateThreadWithScreenshot(threadTitle, messageContent, screenshotBytes, selectedTagIds).Forget();
        }

        /// <summary>
        ///     Sends a plain text message to the Discord feedback webhook.
        /// </summary>
        /// <param name="message">The message content to send.</param>
        public static void Tracking(string message)
        {
            SendToDiscord(message).Forget();
        }

        /// <summary>
        ///     Captures a screenshot from the current frame and sends it along with a message to Discord.
        ///     Must be called from the main thread.
        /// </summary>
        /// <param name="message">The message content to send alongside the screenshot.</param>
        public static void TrackingScreenShot(string message)
        {
            SendScreenshotDiscordOptimized(message).Forget();
        }

        /// <summary>
        ///     Sends a pre-captured screenshot along with a message to the Discord feedback webhook.
        /// </summary>
        /// <param name="message">The message content to send.</param>
        /// <param name="screenshotBytes">Pre-captured PNG screenshot bytes to attach.</param>
        public static void TrackingWithScreenShot(string message, byte[] screenshotBytes)
        {
            SendWithScreenshot(message, screenshotBytes).Forget();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Asynchronously creates a new forum thread with an optional screenshot attachment via Discord Bot API.
        /// </summary>
        /// <param name="threadTitle">Title of the thread.</param>
        /// <param name="messageContent">Body message content.</param>
        /// <param name="pngBytes">PNG image bytes to attach, or null for text-only.</param>
        /// <param name="selectedTagIds">Optional forum tag IDs to apply to the thread.</param>
        /// <returns>A <see cref="UniTaskVoid" /> representing the asynchronous operation.</returns>
        private static async UniTaskVoid CreateThreadWithScreenshot(string threadTitle, string messageContent,
            byte[] pngBytes, string[] selectedTagIds = null)
        {
            var createThreadUrl = $"https://discord.com/api/v10/channels/{_targetChannelId}/threads";

            // Validate thread title: Discord requires 1-100 characters
            var validatedTitle = string.IsNullOrWhiteSpace(threadTitle)
                ? DEFAULT_THREAD_NAME
                : threadTitle.Length > MAX_THREAD_NAME_LENGTH
                    ? threadTitle.Substring(0, MAX_THREAD_NAME_LENGTH)
                    : threadTitle;

            // Validate message content: Discord requires <= 2000 characters
            var validatedContent = string.IsNullOrEmpty(messageContent)
                ? ""
                : messageContent.Length > MAX_MESSAGE_CONTENT_LENGTH
                    ? messageContent.Substring(0, MAX_MESSAGE_CONTENT_LENGTH)
                    : messageContent;

            var safeTitle = validatedTitle
                .Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
            var safeContent = validatedContent
                .Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

            var appliedTagsJson = "";
            if (selectedTagIds != null && selectedTagIds.Length > 0)
            {
                var quotedIds = new StringBuilder();
                for (var i = 0; i < selectedTagIds.Length; i++)
                {
                    if (i > 0) quotedIds.Append(", ");
                    quotedIds.Append($"\"{selectedTagIds[i]}\"");
                }

                appliedTagsJson = $@",
        ""applied_tags"": [{quotedIds}]";
            }

            var jsonPayload = $@"{{
        ""name"": ""{safeTitle}"",
        ""auto_archive_duration"": 1440{appliedTagsJson},
        ""message"": {{
            ""content"": ""{safeContent}""
        }}
    }}";

            var formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormDataSection("payload_json", jsonPayload));

            if (pngBytes != null && pngBytes.Length > 0)
                formData.Add(new MultipartFormFileSection("files[0]", pngBytes, "screenshot.png", "image/png"));
            else
                Debug.LogWarning(
                    "[DiscordTracking] CreateThreadWithScreenshot was called without screenshot bytes. Thread will be created without attachment.");

            using var req = UnityWebRequest.Post(createThreadUrl, formData);
            req.SetRequestHeader("Authorization", $"Bot {_botToken}");

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError($"[DiscordTracking] Error: {req.error} \nServer Response: {req.downloadHandler.text}");
            else
                Debug.Log("[DiscordTracking] Thread created and screenshot uploaded successfully!");
        }

        /// <summary>
        ///     Asynchronously sends a message with a pre-captured screenshot to the feedback webhook.
        ///     Falls back to text-only if no screenshot bytes are provided.
        /// </summary>
        /// <param name="message">The message content to send.</param>
        /// <param name="pngBytes">Pre-captured PNG bytes to attach.</param>
        /// <returns>A <see cref="UniTask" /> representing the asynchronous operation.</returns>
        private static async UniTask SendWithScreenshot(string message, byte[] pngBytes)
        {
            if (pngBytes == null || pngBytes.Length == 0)
            {
                await SendToDiscord(message);
                return;
            }

            var formData = new List<IMultipartFormSection>
            {
                new MultipartFormDataSection("content", message),
                new MultipartFormFileSection("file", pngBytes, "screenshot.png", "image/png")
            };

            using var req = UnityWebRequest.Post(_webhookFeedbackUrl, formData);
            await req.SendWebRequest();
        }

        /// <summary>
        ///     Asynchronously sends a plain text message to the Discord feedback webhook as JSON.
        /// </summary>
        /// <param name="message">The message content to send.</param>
        /// <returns>A <see cref="UniTask" /> representing the asynchronous operation.</returns>
        private static async UniTask SendToDiscord(string message)
        {
            var safeContent = message
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");

            var json = $"{{\"content\": \"{safeContent}\"}}";

            using var req = new UnityWebRequest(_webhookFeedbackUrl, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError(
                    $"[DiscordTracking] SendToDiscord Error: {req.error} \nServer Response: {req.downloadHandler.text}");
        }

        /// <summary>
        ///     Captures a screenshot at the end of the current frame and sends it to Discord via the main webhook.
        ///     Encoding is offloaded to a thread pool to avoid main thread lag.
        ///     Must be called from the main thread.
        /// </summary>
        /// <param name="message">The message content to send alongside the screenshot.</param>
        /// <returns>A <see cref="UniTaskVoid" /> representing the asynchronous operation.</returns>
        private static async UniTaskVoid SendScreenshotDiscordOptimized(string message)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            var request = await AsyncGPUReadback.Request(tex).ToUniTask();

            if (request.hasError)
            {
                Object.Destroy(tex);
                return;
            }

            var width = request.width;
            var height = request.height;
            var rawData = request.GetData<byte>();
            var nativeDataCopy =
                new NativeArray<byte>(rawData, Allocator.Persistent);

            // Clear RAM after GPU readback
            Object.Destroy(tex);

            // Offload PNG encoding to thread pool to avoid main thread lag
            byte[] pngBytes = null;
            await UniTask.SwitchToThreadPool();

            try
            {
                var encodedNative = ImageConversion.EncodeNativeArrayToPNG(nativeDataCopy,
                    GraphicsFormat.R8G8B8A8_SRGB, (uint)width, (uint)height);
                pngBytes = encodedNative.ToArray();
                encodedNative.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DiscordTracking] PNG encoding failed: {ex.Message}");
            }
            finally
            {
                nativeDataCopy.Dispose();
            }

            await UniTask.SwitchToMainThread();

            if (pngBytes != null)
            {
                var formData = new List<IMultipartFormSection>
                {
                    new MultipartFormDataSection("content", message),
                    new MultipartFormFileSection("file", pngBytes, "screenshot.png", "image/png")
                };

                using var req = UnityWebRequest.Post(_webhookUrl, formData);
                await req.SendWebRequest();
            }
        }

        #endregion
    }
}