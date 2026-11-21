using UnityEngine;
using UnityEngine.UI;

namespace CatFight.UI
{
    internal static class GameTypography
    {
        public static void Apply(Text label, int designSize)
        {
            int enlarged = Mathf.RoundToInt(designSize * 1.22f);
            label.fontSize = enlarged;
            label.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(designSize * 0.8f));
            label.resizeTextMaxSize = enlarged;
            label.resizeTextForBestFit = true;
        }
    }
}
