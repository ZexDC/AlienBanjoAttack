using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienBanjoAttack
{
    interface ISprite
    {
        void Update(Playfield game);
        void Draw(SpriteBatch spriteBatch);
        float DistanceFrom(Sprite otherSprite);
        bool CollidedWith(Sprite otherSprite);
        void Reset(Playfield game);
    }
}
