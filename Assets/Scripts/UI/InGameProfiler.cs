#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace CatFight.UI
{
    public sealed class InGameProfiler : MonoBehaviour
    {
        public Text Label { private get; set; }
        private ProfilerRecorder drawCalls;
        private int frames;
        private float elapsed;
        private float longestFrame;

        private void OnEnable()
        {
            frames = 0;
            elapsed = longestFrame = 0f;
            drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        }

        private void OnDisable() => drawCalls.Dispose();

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;
            frames++;
            elapsed += dt;
            longestFrame = Mathf.Max(longestFrame, dt);
            if (elapsed < 0.5f || Label == null) return;

            float averageMs = 1000f * elapsed / frames;
            long memoryMiB = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            string draws = drawCalls.Valid && drawCalls.Count > 0 ? drawCalls.LastValue.ToString() : "—";
            Label.text = $"{frames / elapsed:F0} FPS   •   {averageMs:F1} ms/khung   •   Đỉnh {longestFrame * 1000f:F1} ms\n" +
                         $"Bộ nhớ Unity {memoryMiB} MiB   •   Draw calls {draws}";
            frames = 0;
            elapsed = longestFrame = 0f;
        }
    }
}
#endif
