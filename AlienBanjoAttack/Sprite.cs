using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;
using System.Xml;

namespace AlienBanjoAttack
{
    public class Sprite : ISprite
    {
        public Texture2D SpriteTexture;
        public Color SpriteColor;
        public Rectangle SpriteRectangle;
        public Vector2 SpritePosition;
        public Vector2 OriginalPosition;
        public string SpriteType;
        public bool IsAlive = true;
        public bool IsExploding = false;
        public float ExplosionTime = 0f;

        public Sprite(Texture2D inTexture, Rectangle inRectangle, Vector2 inPosition, Color inColor, String inSpriteType)
        {
            SpriteType = inSpriteType;
            SpriteTexture = inTexture;
            SpriteRectangle = inRectangle;
            SpritePosition = inPosition;
            OriginalPosition = inPosition;
            SpriteColor = inColor;
        }

        public virtual void Update(Playfield game)
        {
            SpriteRectangle.X = (int)(SpritePosition.X);
            SpriteRectangle.Y = (int)(SpritePosition.Y);
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (IsAlive)
            {
                spriteBatch.Draw(SpriteTexture, SpriteRectangle, SpriteColor);
            }
        }

        /// <summary>
        /// Get the distance of a sprite from another sprite
        /// </summary>
        /// <param name="otherSprite">Second sprite to use to calculate the distance</param>
        /// <returns>Distance from the other sprite</returns>
        public float DistanceFrom(Sprite otherSprite)
        {
            float dx = SpriteRectangle.Center.X - otherSprite.SpriteRectangle.Center.X;
            float dy = SpriteRectangle.Center.Y - otherSprite.SpriteRectangle.Center.Y;
            return (float)Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// Check if sprite collided with another sprite
        /// </summary>
        /// <param name="otherSprite">Second sprite to use to calculate the collision</param>
        /// <returns>True if sprites collided</returns>
        public bool CollidedWith(Sprite otherSprite)
        {
            return SpriteRectangle.Intersects(otherSprite.SpriteRectangle);
        }

        /// <summary>
        /// Reset sprite position and state
        /// </summary>
        /// <param name="game">Current game</param>
        public virtual void Reset(Playfield game)
        {
            SpritePosition = OriginalPosition;
            IsAlive = true;
            SpriteRectangle.X = (int)(SpritePosition.X);
            SpriteRectangle.Y = (int)(SpritePosition.Y);
        }
    }

    public class Note : Sprite
    {
        public Note(Texture2D inTexture, Rectangle inRectangle, Vector2 inPosition, Color inColor, String inSpriteType) :
            base(inTexture, inRectangle, inPosition, inColor, inSpriteType)
        {
        }

        public float noteYSpeed = 3.5f;

        public override void Update(Playfield game)
        {
            if (!IsAlive)
            {
                return;
            }

            SpritePosition.Y -= noteYSpeed;

            // Check if note is off the screen
            if (SpritePosition.Y + SpriteRectangle.Height < 0)
            {
                IsAlive = false;
                return;
            }

            // Check if note has hit a banjo
            foreach (Banjo b in game.Banjos)
            {
                if (b.CollidedWith(this))
                {
                    // Ignore dead banjos
                    if (!b.IsAlive)
                    {
                        continue;
                    }
                    IsAlive = false;
                    switch (b.SpriteType)
                    {
                        case "PlainBanjo":
                            b.IsAlive = false;
                            b.IsExploding = true;
                            game.ExplosionSound.Play();
                            game.Score += 10;
                            break;
                        case "HunterBanjo":
                            b.IsAlive = false;
                            b.IsExploding = true;
                            game.ExplosionSound.Play();
                            game.Score += 20;
                            break;
                        case "DeadlyStrummer":
                            if (b.TotalHits < 2)
                            {
                                b.TotalHits++;
                            }
                            else
                            {
                                b.IsAlive = false;
                                b.IsExploding = true;
                                game.ExplosionSound.Play();
                                game.Score += 50;
                            }
                            break;
                        default:
                            break;
                    }
                    if (game.Score > game.HighScore)
                    {
                        game.HighScore = game.Score;
                    }
                }
            }

            base.Update(game);
        }

        public override void Reset(Playfield game)
        {
            base.Reset(game);

            this.IsAlive = false;
        }
    }

    public class Player : Sprite
    {
        public Player(Texture2D inTexture, Rectangle inRectangle, Vector2 inPosition, Color inColor, String inSpriteType) :
            base(inTexture, inRectangle, inPosition, inColor, inSpriteType)
        {
        }

        public float PlayerSpeed = 3f;
        public int Lives = 3;
        public float ShotTimer = 0;
        public bool MovementEnabled = true;
        public float KilledTime = 0f;

        public override void Update(Playfield game)
        {
            if (MovementEnabled)
            {
                // Save the game
                if ((game.CurrentKeyboardState.IsKeyDown(Keys.S)) || (game.CurrentGamePadState.Buttons.X == ButtonState.Pressed))
                {
                    if (game.gameState == Playfield.State.PlayingGame)
                    {
                        if (game.Save("save_game"))
                        {
                            game.GameSaved();
                        }
                    }
                }
                // Move Left
                if ((game.CurrentKeyboardState.IsKeyDown(Keys.Left)) || (game.CurrentGamePadState.DPad.Left == ButtonState.Pressed))
                {
                    SpritePosition.X -= PlayerSpeed;
                    if (SpritePosition.X < 0)
                    {
                        SpritePosition.X = 0;
                    }
                }

                // Move Right
                if ((game.CurrentKeyboardState.IsKeyDown(Keys.Right)) || (game.CurrentGamePadState.DPad.Right == ButtonState.Pressed))
                {
                    SpritePosition.X += PlayerSpeed;
                    if (SpritePosition.X + SpriteRectangle.Width > game.GraphicsDevice.Viewport.Width)
                    {
                        SpritePosition.X = game.GraphicsDevice.Viewport.Width - SpriteRectangle.Width;
                    }
                }

                // Move Up
                if ((game.CurrentKeyboardState.IsKeyDown(Keys.Up)) || (game.CurrentGamePadState.DPad.Up == ButtonState.Pressed))
                {
                    SpritePosition.Y -= PlayerSpeed;
                    if (SpritePosition.Y < 0)
                    {
                        SpritePosition.Y = 0;
                    }
                }

                // Move Down
                if ((game.CurrentKeyboardState.IsKeyDown(Keys.Down)) || (game.CurrentGamePadState.DPad.Down == ButtonState.Pressed))
                {
                    SpritePosition.Y += PlayerSpeed;
                    if (SpritePosition.Y + SpriteRectangle.Height > game.GraphicsDevice.Viewport.Height)
                    {
                        SpritePosition.Y = game.GraphicsDevice.Viewport.Height - SpriteRectangle.Height;
                    }
                }

                // Fire a note that is not being used
                if ((game.CurrentKeyboardState.IsKeyDown(Keys.Space)) || ((game.CurrentGamePadState.Buttons.A == ButtonState.Pressed)))
                {
                    // Add delay in seconds between shots
                    if (ShotTimer + game.ElapsedTime > 0.20f)
                    {
                        foreach (Note n in game.Notes)
                        {
                            if (!n.IsAlive)
                            {
                                n.IsAlive = true;
                                n.SpritePosition.X = this.SpritePosition.X + n.SpriteRectangle.Width;
                                n.SpritePosition.Y = this.SpritePosition.Y;
                                game.ShotSound.Play();
                                // Reset delay after shooting
                                ShotTimer = 0;
                                break;
                            }
                        }
                    }
                    else
                    {
                        ShotTimer += game.ElapsedTime;
                    }
                }
            }
            // Random movement for attract mode
            else
            {
                SpritePosition.X = game.rand.Next(game.GraphicsDevice.Viewport.Width);
            }

            base.Update(game);
        }

        public override void Reset(Playfield game)
        {
            base.Reset(game);

            Lives = 3;
            ShotTimer = 0;
            MovementEnabled = true;
            KilledTime = 0;
        }

    }

    public class Banjo : Sprite
    {
        public Banjo(Texture2D inTexture, Rectangle inRectangle, Vector2 inPosition, Color inColor, String inSpriteType) :
            base(inTexture, inRectangle, inPosition, inColor, inSpriteType)
        {
        }

        public float banjoXSpeed = 0.5f;
        public float banjoYSpeed = 5f;
        public float LiveTime = 0f;
        public int TotalHits = 0;
        private Vector2 lastSpritePosition;

        public override void Update(Playfield game)
        {
            // Freeze banjo
            if (game.EnemyFreezed)
            {
                SpritePosition = lastSpritePosition;
            }
            else
            {

                // Restore the killed banjos to keep playing
                if (!IsAlive && !IsExploding)
                {
                    Reset(game);
                    SpritePosition = new Vector2(game.rand.Next(game.GraphicsDevice.Viewport.Width), game.rand.Next(game.GraphicsDevice.Viewport.Height / 5));
                    game.SpawnSound.Play();
                }

                // Check if Banjo collides with the player
                if ((IsAlive) && (game.Player.SpriteRectangle.Intersects(SpriteRectangle)) && (game.Player.IsAlive))
                {
                    game.Player.IsAlive = false;
                    this.IsAlive = false;
                    game.ExplosionSound.Play();
                    IsExploding = true;
                    game.Player.Lives--;
                    game.MessageString = "You have been killed!";
                    foreach (Note n in game.Notes)
                    {
                        n.IsAlive = false;
                    }
                }

                if (IsExploding)
                {
                    // Stop explosion after X seconds
                    if (ExplosionTime > game.ExplosionDuration)
                    {
                        IsExploding = false;
                        ExplosionTime = 0;
                    }
                    banjoXSpeed = 0;
                    banjoYSpeed = 0;
                    ExplosionTime += game.ElapsedTime;
                }

                // Check screen collisions
                if (SpritePosition.X < 0)
                {
                    SpritePosition.X = 0;
                    banjoXSpeed = Math.Abs(banjoXSpeed);
                    SpritePosition.Y += banjoYSpeed;
                }
                if (SpritePosition.X + SpriteRectangle.Width > game.GraphicsDevice.Viewport.Width)
                {
                    SpritePosition.X = game.GraphicsDevice.Viewport.Width - SpriteRectangle.Width;
                    banjoXSpeed = -Math.Abs(banjoXSpeed);
                    SpritePosition.Y += banjoYSpeed;
                }

                // Check if Banjo reached the bottom
                if ((SpritePosition.Y + game.Player.SpriteRectangle.Height > game.GraphicsDevice.Viewport.Height) && (this.IsAlive))
                {
                    this.IsAlive = false;
                    game.Player.IsAlive = false;
                    game.Player.MovementEnabled = false;
                    foreach (Note n in game.Notes)
                    {
                        n.IsAlive = false;
                    }
                    if (game.gameState == Playfield.State.PlayingGame)
                    {
                        game.GameOver();
                    }
                }
                lastSpritePosition = SpritePosition;
            }

            base.Update(game);
        }

        public override void Reset(Playfield game)
        {
            base.Reset(game);
        }
    }

    public class PlainBanjo : Banjo
    {
        public PlainBanjo(Texture2D inTexture, Rectangle inRectangle, Vector2 inPosition, Color inColor, String inSpriteType) :
            base(inTexture, inRectangle, inPosition, inColor, inSpriteType)
        {
        }

        public override void Update(Playfield game)
        {
            SpritePosition.X += banjoXSpeed;

            base.Update(game);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

        }

        public override void Reset(Playfield game)
        {
            base.Reset(game);

            banjoXSpeed = 0.5f;
            banjoYSpeed = 5f;
        }
    }

    public class HunterBanjo : Banjo
    {
        public HunterBanjo(Texture2D inTexture, Rectangle inRectangle, Vector2 inPosition, Color inColor, String inSpriteType) :
            base(inTexture, inRectangle, inPosition, inColor, inSpriteType)
        {
        }

        private float MasterSpeed = 0.20f;
        public bool ChaseActive = false;

        public override void Update(Playfield game)
        {
            if (!ChaseActive) // Wait 5 seconds before attacking the player
            {
                SpritePosition.X += banjoXSpeed;
                LiveTime += game.ElapsedTime;
                if (LiveTime > 5f)
                {
                    ChaseActive = true;
                }
            }
            else // Start chasing the player
            {
                if (SpritePosition.X < game.Player.SpritePosition.X)
                {
                    SpritePosition.X += MasterSpeed;
                }
                else
                {
                    SpritePosition.X -= MasterSpeed;
                }

                if (SpritePosition.Y < game.Player.SpritePosition.Y)
                {
                    SpritePosition.Y += MasterSpeed;
                }
                else
                {
                    SpritePosition.Y -= MasterSpeed;
                }
            }

            if (IsExploding)
            {
                // Stop explosion after 5 seconds
                if (ExplosionTime + game.ElapsedTime > 5f)
                {
                    IsExploding = false;
                }
                MasterSpeed = 0;
                ExplosionTime += game.ElapsedTime;
            }

            base.Update(game);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }

        public override void Reset(Playfield game)
        {
            base.Reset(game);

            banjoXSpeed = 0.5f;
            banjoYSpeed = 5f;
            LiveTime = 0;
            MasterSpeed = 0.20f;
            ChaseActive = false;
        }
    }

    public class DeadlyStrummer : Banjo
    {
        public DeadlyStrummer(Texture2D inTexture, Rectangle inRectangle, Vector2 inPosition, Color inColor, String inSpriteType) :
            base(inTexture, inRectangle, inPosition, inColor, inSpriteType)
        {
        }

        private float MasterSpeed = 0.25f;

        public override void Update(Playfield game)
        {
            if (SpritePosition.X < game.Player.SpritePosition.X)
            {
                SpritePosition.X += MasterSpeed;
            }
            else
            {
                SpritePosition.X -= MasterSpeed;
            }

            if (SpritePosition.Y < game.Player.SpritePosition.Y)
            {
                SpritePosition.Y += MasterSpeed;
            }
            else
            {
                SpritePosition.Y -= MasterSpeed;
            }

            if (IsExploding)
            {
                // Stop explosion after 5 seconds
                if (ExplosionTime + game.ElapsedTime > 5f)
                {
                    IsExploding = false;
                }
                MasterSpeed = 0;
                ExplosionTime += game.ElapsedTime;
            }

            base.Update(game);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }

        public override void Reset(Playfield game)
        {
            base.Reset(game);

            MasterSpeed = 0.25f;
            TotalHits = 0;
        }
    }

}