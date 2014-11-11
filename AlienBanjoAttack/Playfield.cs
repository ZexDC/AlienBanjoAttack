using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Threading;
using System.Xml;
using System.Text;

namespace AlienBanjoAttack
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Playfield : Microsoft.Xna.Framework.Game
    {
        public enum State
        {
            None,
            AttractMode,
            PlayingGame,
            GameOver
        }

        // Set game start to attract mode
        public State gameState;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        SpriteFont font;
        public Player Player;
        public List<Sprite> Sprites = new List<Sprite>();
        public List<Banjo> Banjos = new List<Banjo>();
        public List<Note> Notes = new List<Note>();
        public List<Sprite> Explosions = new List<Sprite>();

        public string MessageString = "";
        public string MessageScore = "";
        public string MessageHighScore = "";
        public string MessageWaitInput = "";
        public string MessageSave = "";
        public string MessageLoad = "";
        Vector2 MessagePosition;

        public KeyboardState CurrentKeyboardState;
        public KeyboardState LastKeyboardState;
        public GamePadState CurrentGamePadState;
        public GamePadState LastGamePadState;

        public Random rand = new Random();
        public float ElapsedTime;
        public float GameOverTime;
        public float GameSavedTime;

        public int Score;
        public int HighScore;
        public bool NewGame;
        public bool ShowGameSaved;
        public bool IsGameLoaded;
        public bool EnemyFreezed;

        public Song MusicSound;
        public SoundEffect SpawnSound;
        public SoundEffect ShotSound;
        public SoundEffect ExplosionSound;

        private const int MAX_PLAIN_BANJOS = 70;
        private const int MAX_HUNTER_BANJOS = 20;
        private const int MAX_DEADLY_BANJOS = 10;
        private const int MAX_NOTES = 50;
        private const float ATTRACT_DELAY = 10f;
        private const float SAVE_DELAY = 3f;
        private const float RESPAWN_TIME = 2f;
        public float ExplosionDuration = 1.5f;

        public Playfield()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.IsFullScreen = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            // Initialize variables for this session
            HighScore = 0;
            ElapsedTime = 0;
            gameState = State.None;
            ShowGameSaved = false;
            IsGameLoaded = false;
            MessageLoad = "Press L/Pad Y to load last saved game";

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Texture2D texture;
            Rectangle rectangle;
            Vector2 vector;

            // Load sounds
            SpawnSound = Content.Load<SoundEffect>("sounds/spawnbanjo");
            ShotSound = Content.Load<SoundEffect>("sounds/shot");
            ExplosionSound = Content.Load<SoundEffect>("sounds/explosion");
            MusicSound = Content.Load<Song>("sounds/music");
            MediaPlayer.Play(MusicSound);
            MediaPlayer.IsRepeating = true;

            // Load background
            texture = Content.Load<Texture2D>("textures/background");
            rectangle = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            vector = new Vector2(0, 0);
            Sprites.Add(new Sprite(texture, rectangle, vector, Color.White, "Background"));

            // Load SpriteFont and score/lives position
            font = Content.Load<SpriteFont>("MessageFont");
            MessagePosition = new Vector2(20, GraphicsDevice.Viewport.Height - font.LineSpacing - 20);

            // Load Player
            texture = Content.Load<Texture2D>("textures/accordion");
            rectangle = rectScale(texture, 10);
            vector = new Vector2(GraphicsDevice.Viewport.Width / 2 - rectangle.Width / 2, GraphicsDevice.Viewport.Height - rectangle.Height);
            Player = new Player(texture, rectangle, vector, Color.White, "Player");
            Sprites.Add(Player);

            // Load Notes
            texture = Content.Load<Texture2D>("textures/note");
            rectangle = rectScale(texture, 30);
            for (int i = 0; i < MAX_NOTES; i++)
            {
                vector = new Vector2(-rectangle.Width, -rectangle.Height);
                Note newNote = new Note(texture, rectangle, vector, Color.White, "Note");
                newNote.IsAlive = false;
                Sprites.Add(newNote);
                Notes.Add(newNote);
            }

            // Load Plain Banjos
            texture = Content.Load<Texture2D>("textures/PlainBanjo");
            rectangle = rectScale(texture, 20);
            for (int i = 0; i < MAX_PLAIN_BANJOS; i++)
            {
                vector = new Vector2(rand.Next(GraphicsDevice.Viewport.Width), rand.Next(GraphicsDevice.Viewport.Height / 5));
                PlainBanjo newBanjo = new PlainBanjo(texture, rectangle, vector, Color.White, "PlainBanjo");
                Sprites.Add(newBanjo);
                Banjos.Add(newBanjo);
            }

            // Load Hunter Banjos
            texture = Content.Load<Texture2D>("textures/AttackerBanjo");
            rectangle = rectScale(texture, 20);
            for (int i = 0; i < MAX_HUNTER_BANJOS; i++)
            {
                vector = new Vector2(rand.Next(GraphicsDevice.Viewport.Width), rand.Next(GraphicsDevice.Viewport.Height / 5));
                HunterBanjo newBanjo = new HunterBanjo(texture, rectangle, vector, Color.LightGray, "HunterBanjo");
                Sprites.Add(newBanjo);
                Banjos.Add(newBanjo);
            }

            // Load Deadly Strummer Banjos
            texture = Content.Load<Texture2D>("textures/DeadlyStrummer");
            rectangle = rectScale(texture, 20);
            for (int i = 0; i < MAX_DEADLY_BANJOS; i++)
            {
                vector = new Vector2(rand.Next(GraphicsDevice.Viewport.Width), rand.Next(GraphicsDevice.Viewport.Height / 5));
                DeadlyStrummer newBanjo = new DeadlyStrummer(texture, rectangle, vector, Color.White, "DeadlyStrummer");
                Sprites.Add(newBanjo);
                Banjos.Add(newBanjo);
            }

            // Load Attract mode image title
            texture = Content.Load<Texture2D>("textures/AttractTitle");
            rectangle = new Rectangle(0, GraphicsDevice.Viewport.Height - texture.Height, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height / 2);
            vector = new Vector2(0, 0);
            Sprites.Add(new Sprite(texture, rectangle, vector, Color.White, "AttractTitle"));

            // Load explosions
            texture = Content.Load<Texture2D>("textures/explosions/explosion1");
            rectangle = rectScale(texture, 20);
            vector = new Vector2(-rectangle.Width, -rectangle.Height);
            Sprites.Add(new Sprite(texture, rectangle, vector, Color.White, "Explosion"));
            Explosions.Add(new Sprite(texture, rectangle, vector, Color.White, "Explosion"));
            texture = Content.Load<Texture2D>("textures/explosions/explosion2");
            rectangle = rectScale(texture, 20);
            vector = new Vector2(-rectangle.Width, -rectangle.Height);
            Sprites.Add(new Sprite(texture, rectangle, vector, Color.White, "Explosion"));
            Explosions.Add(new Sprite(texture, rectangle, vector, Color.White, "Explosion"));
            texture = Content.Load<Texture2D>("textures/explosions/explosion3");
            rectangle = rectScale(texture, 20);
            vector = new Vector2(-rectangle.Width, -rectangle.Height);
            Sprites.Add(new Sprite(texture, rectangle, vector, Color.White, "Explosion"));
            Explosions.Add(new Sprite(texture, rectangle, vector, Color.White, "Explosion"));
            texture = Content.Load<Texture2D>("textures/explosions/explosion4");
            rectangle = rectScale(texture, 20);
            vector = new Vector2(-rectangle.Width, -rectangle.Height);
            Sprites.Add(new Sprite(texture, rectangle, vector, Color.White, "Explosion"));
            Explosions.Add(new Sprite(texture, rectangle, vector, Color.White, "Explosion"));

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            CurrentKeyboardState = Keyboard.GetState();
            CurrentGamePadState = GamePad.GetState(PlayerIndex.One);

            ElapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Allows the game to exit
            if ((CurrentGamePadState.Buttons.Back == ButtonState.Pressed) || (CurrentKeyboardState.IsKeyDown(Keys.Escape)))
                this.Exit();

            switch (gameState)
            {
                case State.AttractMode:
                    foreach (Sprite s in Sprites)
                    {
                        s.Update(this);
                    }
                    // Check if there is a saved game to resume and wait for input
                    if ((!IsGameLoaded) && ((CurrentKeyboardState.IsKeyDown(Keys.L)) || (CurrentGamePadState.IsButtonDown(Buttons.Y))))
                    {
                        if (Load("save_game"))
                        {
                            GameLoaded();
                        }
                    }
                    // Start new game if Input key detected
                    if (((CurrentKeyboardState.IsKeyDown(Keys.Enter)) && (LastKeyboardState.IsKeyUp(Keys.Enter))) || ((CurrentGamePadState.IsButtonDown(Buttons.Start)) && (LastGamePadState.IsButtonUp(Buttons.Start))))
                    {
                        StartNewGame();
                    }
                    break;

                case State.PlayingGame:
                    IsGameLoaded = false;
                    if (NewGame)
                    {
                        Score = 0;
                        foreach (Sprite s in Sprites)
                        {
                            s.Reset(this);
                        }
                        NewGame = false;
                    }
                    if (!Player.IsAlive)
                    {
                        if (Player.Lives > 0)
                            MessageString = "Respawning...";
                        PlayerKilled();
                        Player.KilledTime += ElapsedTime;
                    }
                    else
                    {
                        // Update Score
                        MessageString = String.Format("Lives: {0} Score: {1}", Player.Lives, Score);
                    }
                    foreach (Sprite s in Sprites)
                    {
                        s.Update(this);
                    }
                    // Check the time that the save message has been shown
                    if (ShowGameSaved)
                    {
                        if (GameSavedTime + ElapsedTime > SAVE_DELAY)
                        {
                            ShowGameSaved = false;
                        }
                        GameSavedTime += ElapsedTime;
                    }
                    break;

                case State.GameOver:
                    foreach (Sprite s in Sprites)
                    {
                        s.Update(this);
                    }
                    // Switch to attract mode after X seconds or if Input key detected
                    if ((CurrentKeyboardState.IsKeyDown(Keys.Enter)) || (CurrentGamePadState.IsButtonDown(Buttons.Start)) || (GameOverTime + ElapsedTime > ATTRACT_DELAY))
                    {
                        AttractMode();
                    }
                    GameOverTime += ElapsedTime;
                    break;
                default:
                    // Star game in attract mode
                    AttractMode();
                    break;
            }

            base.Update(gameTime);

            LastKeyboardState = CurrentKeyboardState;
            LastGamePadState = CurrentGamePadState;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();

            switch (gameState)
            {
                case State.AttractMode:
                    // Draw each sprite in the list
                    foreach (Sprite s in Sprites)
                    {
                        s.Draw(spriteBatch);
                    }
                    // Draw High Score message
                    DrawText(spriteBatch, MessageHighScore, Color.Black, Color.Red, 1f, 0f, new Vector2(CenterMessagePositionX(MessageHighScore), graphics.GraphicsDevice.Viewport.Height / 2 + font.LineSpacing * 1.5f));
                    // Draw Wait input message
                    DrawText(spriteBatch, MessageWaitInput, Color.Black, Color.White, 1f, 0f, new Vector2(CenterMessagePositionX(MessageWaitInput), graphics.GraphicsDevice.Viewport.Height / 2 + font.LineSpacing * 4.5f));
                    // Draw Load game message
                    DrawText(spriteBatch, MessageLoad, Color.Black, Color.White, 1f, 0f, new Vector2(CenterMessagePositionX(MessageLoad), graphics.GraphicsDevice.Viewport.Height / 2 + font.LineSpacing * 6.0f));
                    break;

                case State.PlayingGame:
                    // Draw each sprite in the list except the title
                    foreach (Sprite s in Sprites)
                    {
                        if (!(s.SpriteType == "AttractTitle"))
                        {
                            s.Draw(spriteBatch);
                            if (s.IsExploding)
                            {
                                DrawExplosion(spriteBatch, s.SpriteRectangle, s);
                            }

                        }

                    }
                    // Draw the player
                    Player.Draw(spriteBatch);
                    // Draw the score or message
                    DrawText(spriteBatch, MessageString, Color.Black, Color.White, 1f, 0f, MessagePosition);
                    if (ShowGameSaved)
                    {
                        DrawText(spriteBatch, MessageSave, Color.Black, Color.White, 1f, 0f, new Vector2(GraphicsDevice.Viewport.Width - font.MeasureString(MessageSave).X - 20, GraphicsDevice.Viewport.Height - font.LineSpacing - 20));
                    }
                    break;

                case State.GameOver:
                    // Draw each sprite in the list except the title
                    foreach (Sprite s in Sprites)
                    {
                        if (!(s.SpriteType == "AttractTitle"))
                            s.Draw(spriteBatch);
                    }
                    // Draw the player
                    Player.Draw(spriteBatch);
                    // Draw game over message
                    DrawText(spriteBatch, MessageString, Color.Black, Color.Yellow, 1f, 0f, new Vector2(CenterMessagePositionX(MessageString), GraphicsDevice.Viewport.Height / 2 - font.LineSpacing * 2));
                    // Draw Score message
                    DrawText(spriteBatch, MessageScore, Color.Black, Color.Red, 1f, 0f, new Vector2(CenterMessagePositionX(MessageScore), GraphicsDevice.Viewport.Height / 2));
                    // Draw High Score message
                    DrawText(spriteBatch, MessageHighScore, Color.Black, Color.Red, 1f, 0f, new Vector2(CenterMessagePositionX(MessageHighScore), GraphicsDevice.Viewport.Height / 2 + font.LineSpacing * 1));
                    // Draw Wait input message
                    DrawText(spriteBatch, MessageWaitInput, Color.Black, Color.White, 1f, 0f, new Vector2(CenterMessagePositionX(MessageWaitInput), GraphicsDevice.Viewport.Height / 2 + font.LineSpacing * 3));
                    break;

                default:
                    break;
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Resize a given texture by a factor number
        /// </summary>
        /// <param name="texture">Texture to resize</param>
        /// <param name="factor">Value used for scaling, 1 for original size</param>
        /// <returns>Resized Rectangle</returns>
        private Rectangle rectScale(Texture2D texture, float factor)
        {
            int width = (int)(GraphicsDevice.Viewport.Width / factor);
            float aspect = texture.Width / (float)texture.Height;
            return new Rectangle(0, 0, width, (int)(width / aspect));
        }

        /// <summary>
        /// Draws the explosion effect when a banjo has been killed
        /// </summary>
        /// <param name="sb">Game SpriteBatch</param>
        /// <param name="rectangle">Rectangle to draw</param>
        /// <param name="s">Banjo killed</param>
        public void DrawExplosion(SpriteBatch sb, Rectangle rectangle, Sprite s)
        {
            if (s.ExplosionTime < ExplosionDuration / 4)
            {
                sb.Draw(Explosions[0].SpriteTexture, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width + 20, rectangle.Height - 20), Color.White);
            }
            else if (s.ExplosionTime < ExplosionDuration / 3)
            {
                sb.Draw(Explosions[1].SpriteTexture, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width + 20, rectangle.Height - 20), Color.White);
            }
            else if (s.ExplosionTime < ExplosionDuration / 2)
            {
                sb.Draw(Explosions[2].SpriteTexture, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width + 20, rectangle.Height - 20), Color.White);
            }
            else if (s.ExplosionTime < ExplosionDuration)
            {
                sb.Draw(Explosions[3].SpriteTexture, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width + 20, rectangle.Height - 20), Color.White);
            }
        }

        /// <summary>
        /// Check if player has more lives and make it respawn or end the game
        /// </summary>
        public void PlayerKilled()
        {
            // Disable movement input
            Player.MovementEnabled = false;
            EnemyFreezed = true;

            if (Player.KilledTime > RESPAWN_TIME)
            {
                // Reset the kill time for respawn
                Player.KilledTime = 0;
                // Unfreeze enemies
                EnemyFreezed = false;
                // Keep playing if player has lives
                if (Player.Lives > 0)
                {
                    // Enable movement input
                    Player.MovementEnabled = true;
                    // Move the player to the original position and bring it back to life
                    Player.SpritePosition = Player.OriginalPosition;
                    Player.IsAlive = true;
                }
                else
                {
                    // Move dead Player outside screen, so Banjos keep chasing till the bottom
                    Player.SpritePosition = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height + Player.SpriteRectangle.Height);
                    GameOver();
                }
            }
        }

        /// <summary>
        /// Set the game state to GameOver and set relative values
        /// </summary>
        public void GameOver()
        {
            GameOverTime = 0;
            gameState = State.GameOver;
            MessageString = "Game Over";
            MessageScore = String.Format("Your score: {0}", Score);
            MessageHighScore = String.Format("High score: {0}", HighScore);
            MessageWaitInput = "Press Enter/Pad Start to continue";
        }

        /// <summary>
        /// Set the game state to AttractMode and set relative values
        /// </summary>
        public void AttractMode()
        {
            gameState = State.AttractMode;
            foreach (Sprite s in Sprites)
            {
                s.Reset(this);
            }
            // Hide player
            Player.IsAlive = false;
            // Disable movement input
            Player.MovementEnabled = false;
            // Move dead Player outside screen, so Banjos keep chasing till the bottom
            Player.SpritePosition = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height + Player.SpriteRectangle.Height);
            MessageHighScore = String.Format("High score: {0}", HighScore);
            MessageWaitInput = "Press Enter/Pad Start to start a new game";
        }

        /// <summary>
        /// Reset values to start a new game
        /// </summary>
        public void StartNewGame()
        {
            NewGame = true;
            foreach (Sprite s in Sprites)
            {
                s.Reset(this);
            }
            Score = 0;
            gameState = State.PlayingGame;
        }

        /// <summary>
        /// Set game saved message values
        /// </summary>
        public void GameSaved()
        {
            GameSavedTime = 0;
            MessageSave = "Game saved";
            ShowGameSaved = true;
        }

        /// <summary>
        /// Set game loaded message values
        /// </summary>
        public void GameLoaded()
        {
            GameSavedTime = 0;
            MessageSave = "Game loaded";
            ShowGameSaved = true;
            IsGameLoaded = true;
            Player.MovementEnabled = true;
            gameState = State.PlayingGame;
        }

        /// <summary>
        /// Get the message position centered on X axis
        /// </summary>
        /// <param name="message">Text to center</param>
        /// <returns>Centered position of the text on X axis</returns>
        public float CenterMessagePositionX(string message)
        {
            return graphics.GraphicsDevice.Viewport.Width / 2 - font.MeasureString(message).X / 2;
        }

        /// <summary>
        /// Get the message position centered on Y axis
        /// </summary>
        /// <param name="message">Text to center</param>
        /// <returns>Centered position of the text on Y axis</returns>
        public float CenterMessagePositionY(string message)
        {
            return graphics.GraphicsDevice.Viewport.Height / 2 - font.MeasureString(message).Y / 2;
        }

        /// <summary>
        /// Draw the text with a custom outline
        /// </summary>
        /// <param name="sb">Draw spritebatch</param>
        /// <param name="text">Text to draw</param>
        /// <param name="backColor">Outline color</param>
        /// <param name="frontColor">Text color</param>
        /// <param name="scale">Text scale</param>
        /// <param name="rotation">Text rotation</param>
        /// <param name="position">Text position</param>
        private void DrawText(SpriteBatch sb, string text, Color backColor, Color frontColor, float scale, float rotation, Vector2 position)
        {
            Vector2 origin = new Vector2(0, 0);

            // Draw 4 offset copies of text to create outline
            sb.DrawString(font, text, position + new Vector2(1.3f * scale, 1.3f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
            sb.DrawString(font, text, position + new Vector2(-1.3f * scale, -1.3f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
            sb.DrawString(font, text, position + new Vector2(-1.3f * scale, 1.3f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
            sb.DrawString(font, text, position + new Vector2(1.3f * scale, -1.3f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
            //This is the top layer which we draw in the middle so it covers all the other texts except that displacement
            sb.DrawString(font, text, position, frontColor, rotation, origin, scale, SpriteEffects.None, 1f);
        }

        /// <summary>
        /// Check for any user input
        /// </summary>
        /// <param name="state">Keyboard state</param>
        /// <returns>True if any key is pressed</returns>
        public bool IsAnyKeyPressed(KeyboardState state)
        {
            Keys[] keys = state.GetPressedKeys();
            return keys.Length == 0 || (keys.Length == 1 && keys[0] == Keys.None);
        }

        /// <summary>
        /// Save the current game
        /// </summary>
        /// <param name="fileName">File path</param>
        /// <returns>True if game saved successfully</returns>
        public bool Save(string fileName)
        {
            try
            {
                XmlTextWriter textWriter = new XmlTextWriter(fileName, Encoding.ASCII);
                textWriter.Formatting = Formatting.Indented;
                textWriter.WriteStartDocument();
                textWriter.WriteStartElement("root");

                textWriter.WriteStartElement("playfield");
                textWriter.WriteAttributeString("game", "Alien Banjo Attack");

                textWriter.WriteStartElement("highscore");
                textWriter.WriteElementString("player", "Player 1");
                textWriter.WriteElementString("score", Score.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("player");
                textWriter.WriteElementString("type", Player.SpriteType);
                textWriter.WriteElementString("status", Player.IsAlive.ToString());
                textWriter.WriteElementString("lives", Player.Lives.ToString());
                textWriter.WriteElementString("position_X", Player.SpritePosition.X.ToString());
                textWriter.WriteElementString("position_Y", Player.SpritePosition.Y.ToString());
                textWriter.WriteEndElement();

                foreach (Note n in Notes)
                {
                    textWriter.WriteStartElement("notes");
                    textWriter.WriteElementString("type", n.SpriteType);
                    textWriter.WriteElementString("status", n.IsAlive.ToString());
                    textWriter.WriteElementString("position_X", n.SpritePosition.X.ToString());
                    textWriter.WriteElementString("position_Y", n.SpritePosition.Y.ToString());
                    textWriter.WriteEndElement();
                }

                foreach (Banjo b in Banjos)
                {
                    textWriter.WriteStartElement("banjos");
                    textWriter.WriteElementString("type", b.SpriteType);
                    textWriter.WriteElementString("status", b.IsAlive.ToString());
                    textWriter.WriteElementString("position_X", b.SpritePosition.X.ToString());
                    textWriter.WriteElementString("position_Y", b.SpritePosition.Y.ToString());
                    textWriter.WriteElementString("speed_X", b.banjoXSpeed.ToString());
                    textWriter.WriteElementString("speed_Y", b.banjoYSpeed.ToString());
                    textWriter.WriteElementString("live_time", b.LiveTime.ToString());
                    textWriter.WriteElementString("hits", b.TotalHits.ToString());
                    textWriter.WriteEndElement();
                }

                textWriter.WriteEndDocument();
                textWriter.Close();
            }
            catch
            {
                MessageSave = "Game could not be saved";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Load the last saved game
        /// </summary>
        /// <param name="fileName">File path</param>
        /// <returns>True if game loaded successfully</returns>
        public bool Load(string fileName)
        {
            int noteId = 0;
            int banjoId = 0;
            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(fileName);
                XmlElement rootElement = document.DocumentElement;

                if (rootElement.Name != "root")
                {
                    Console.WriteLine("Save game file is corrupted.");
                    return false;
                }
                foreach (XmlElement post in rootElement["playfield"].ChildNodes)
                {
                    if (post.Name == "highscore")
                    {
                        foreach (XmlElement e in post.ChildNodes)
                        {
                            if (e.Name == "score")
                                Score = int.Parse(e.FirstChild.Value);
                        }
                    }
                    if (post.Name == "player")
                    {
                        foreach (XmlElement e in post.ChildNodes)
                        {
                            if (e.Name == "type")
                            {
                                Player.SpriteType = e.FirstChild.Value;
                                continue;
                            }
                            if (e.Name == "status")
                            {
                                Player.IsAlive = bool.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "lives")
                            {
                                Player.Lives = int.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "position_X")
                            {
                                Player.SpritePosition.X = float.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "position_Y")
                            {
                                Player.SpritePosition.Y = float.Parse(e.FirstChild.Value);
                                continue;
                            }
                        }
                    }
                    if (post.Name == "notes")
                    {

                        foreach (XmlElement e in post.ChildNodes)
                        {
                            if (e.Name == "type")
                            {
                                Notes[noteId].SpriteType = e.FirstChild.Value;
                                continue;
                            }
                            if (e.Name == "status")
                            {
                                Notes[noteId].IsAlive = bool.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "position_X")
                            {
                                Notes[noteId].SpritePosition.X = float.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "position_Y")
                            {
                                Notes[noteId].SpritePosition.Y = float.Parse(e.FirstChild.Value);
                                //id++; // Increase id only when last parameter is being loaded
                                continue;
                            }
                        }
                        noteId++;
                    }
                    if (post.Name == "banjos")
                    {
                        //int id = 0;
                        foreach (XmlElement e in post.ChildNodes)
                        {
                            if (e.Name == "type")
                            {
                                Banjos[banjoId].SpriteType = e.FirstChild.Value;
                                continue;
                            }
                            if (e.Name == "status")
                            {
                                Banjos[banjoId].IsAlive = bool.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "position_X")
                            {
                                Banjos[banjoId].SpritePosition.X = float.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "position_Y")
                            {
                                Banjos[banjoId].SpritePosition.Y = float.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "speed_X")
                            {
                                Banjos[banjoId].banjoXSpeed = float.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "speed_Y")
                            {
                                Banjos[banjoId].banjoYSpeed = float.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "live_time")
                            {
                                Banjos[banjoId].LiveTime = float.Parse(e.FirstChild.Value);
                                continue;
                            }
                            if (e.Name == "hits")
                            {
                                Banjos[banjoId].TotalHits = int.Parse(e.FirstChild.Value);
                                //id++; // Increase id only when last parameter is being loaded
                                continue;
                            }

                        }
                        banjoId++;
                    }
                }
            }
            catch
            {
                MessageSave = "Save game file not found or corrupted";
                return false;
            }
            return true;
        }

        public void Menu()
        {
            // TODO for Version 2
        }
    }

}