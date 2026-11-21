using CatFight.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace CatFight.UI
{
    public sealed class PetStarGraphic : Image
    {
        public void SetHalves(int halves)
        {
            var states = RuntimeSpriteBank.GetHorizontalSheet("Sprites/UI/pet_stars", 3);
            sprite = states[Mathf.Clamp(halves, 0, 2)];
            preserveAspect = true;
        }
    }
}
