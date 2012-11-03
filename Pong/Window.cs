// Pong by Jonny Li (jonnyli1125) is licensed under a Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// You may obtain a copy of it here: http://creativecommons.org/licenses/by-nc-sa/3.0/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Net;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using BeginMode = OpenTK.Graphics.OpenGL.BeginMode;
using ClearBufferMask = OpenTK.Graphics.OpenGL.ClearBufferMask;
using GL = OpenTK.Graphics.OpenGL.GL;
using Timer = System.Timers.Timer;

namespace Pong
{
    public class Window : GameWindow
    {
        PrivateFontCollection pfc = new PrivateFontCollection();
        List<string> MenuItems = new List<string>() { "Player vs Computer", "Player vs Player", "Instructions" };
        int HoveredItem = 0; // 0 none, rest are (index of item in menulist + 1)
        int CurrentPage = 0; // 0 start menu, 1 playervcom, 2 playervplayer, 3 instructions

        public Window() : base(800, 600, GraphicsMode.Default, "Pong") { }

        const int InitialBallSpeed = 8, InitialLeftPaddleSpeed = 12, InitialRightPaddleSpeed = 8;
        Timer SpeedTimer = new Timer(4000);

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.ClearColor(Color.White);

            GL.LineWidth(1.5f);

            if (!Directory.Exists("Fonts")) Directory.CreateDirectory("Fonts");
            List<string> fonts = new List<string>() { "BEBAS___.ttf" };
            foreach (string font in fonts)
            {
                if (!File.Exists("Fonts/" + font))
                {
                    try
                    {
                        using (WebClient w = new WebClient())
                            w.DownloadFile("http://dev.mcdawn.com/fonts/" + font, "Fonts/" + font);
                    }
                    catch { }
                }
                pfc.AddFontFile("Fonts/" + font);
            }

            Border = new List<Vector2>(4) { new Vector2(20, 20), new Vector2(20, ClientRectangle.Height - 20), new Vector2(ClientRectangle.Width - 20, ClientRectangle.Height - 20), new Vector2(ClientRectangle.Width - 20, 20) };
            PaddleLeft = new RectangleF(40, (ClientRectangle.Height / 2) - 80, 20, 160);
            PaddleRight = new RectangleF(ClientRectangle.Width - 60, (ClientRectangle.Height / 2) - 80, 20, 160);
            BallPosition = new Vector2(ClientRectangle.Width / 2, ClientRectangle.Height / 2);

            SpeedTimer.Elapsed += delegate {
                BallSpeed += BallSpeedIncrement;
                LeftPaddleSpeed += LeftPaddleSpeedIncrement;
                RightPaddleSpeed += (CurrentPage == 1) ? RightPaddleSpeedIncrement + 0.35f : RightPaddleSpeedIncrement;
            };

            BallXDirection = Random.Next(-5, 6) > 0 ? 1 : -1;
            BallYDirection = Random.Next(-5, 6) > 0 ? 1 : -1;

            Mouse.ButtonDown += (object sender, MouseButtonEventArgs mbe) => {
                if (CurrentPage == 0)
                {
                    if (!IsHoveringOnMenuItem()) HoveredItem = 0;
                    CurrentPage = HoveredItem;
                }
            };

            Keyboard.KeyDown += (object sender, KeyboardKeyEventArgs kke) => {
                if (kke.Key == Key.Escape) { CurrentPage = 0; ResetGame(); }
                if (kke.Key == Key.Space && (CurrentPage == 1 || CurrentPage == 2)) GameActive = !GameActive;
                if (kke.Key == Key.H && (CurrentPage == 1 || CurrentPage == 2))
                {
                    PaddleLeft = (PaddleLeft.Height > 160) ? new RectangleF(40, (ClientRectangle.Height / 2) - 80, 20, 160) : new RectangleF(40, 20, 20, ClientRectangle.Height - 40);
                    if (CurrentPage == 2) PaddleRight = (PaddleRight.Height > 160) ? new RectangleF(ClientRectangle.Width - 60, (ClientRectangle.Height / 2) - 80, 20, 160) : new RectangleF(ClientRectangle.Width - 60, 20, 20, ClientRectangle.Height - 40);
                }
            };
        }

        public FontFamily GetFontFamily(string fontName)
        {
            for (int i = 0; i < pfc.Families.Length; i++)
                if (pfc.Families[i].Name == fontName)
                    return pfc.Families[i];
            return null;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(this.ClientRectangle.Left, this.ClientRectangle.Right,
                this.ClientRectangle.Bottom, this.ClientRectangle.Top, -1.0, 1.0);
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            Border = new List<Vector2>(4) { new Vector2(20, 20), new Vector2(20, ClientRectangle.Height - 20), new Vector2(ClientRectangle.Width - 20, ClientRectangle.Height - 20), new Vector2(ClientRectangle.Width - 20, 20) };
            PaddleLeft = new RectangleF(40, (ClientRectangle.Height / 2) - 80, 20, 160);
            PaddleRight = new RectangleF(ClientRectangle.Width - 60, (ClientRectangle.Height / 2) - 80, 20, 160);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (CurrentPage == 0) Title = "Pong | Start Menu";
            else if (CurrentPage == 1) Title = "Pong | Singleplayer" + (GameActive ? " : " + PointsLeft + " - " + PointsRight : "");
            else if (CurrentPage == 2) Title = "Pong | Multiplayer : " + (GameActive ? " : " + PointsLeft + " - " + PointsRight : "");
            else if (CurrentPage == 3) Title = "Pong | Instructions";
            else CurrentPage = 0;

            MoveBall();

            if (Keyboard[Key.W]) MovePaddleLeft(-1);
            if (Keyboard[Key.S]) MovePaddleLeft(1);
            if (Keyboard[Key.Up]) MovePaddleRight(-1);
            if (Keyboard[Key.Down]) MovePaddleRight(1);
        }

#pragma warning disable 0612
        // Supressing warnings, so it will stop bugging me about the TextPrinter being obsolete / deprecated T_T
        TextPrinter TextPrinter = new TextPrinter(TextQuality.High);
#pragma warning restore 0612
        Font font;
        RectangleF RectangleSpace;

        List<Vector2> Border;
        RectangleF PaddleLeft, PaddleRight;
        int PointsLeft = 0, PointsRight = 0;
        float BallRadius = 20.0f, BallXDirection = 1.0f, BallYDirection = 1.0f, BallSpeed = 8.0f, LeftPaddleSpeed = 12.0f, RightPaddleSpeed = 8.0f, BallSpeedIncrement = 1.0f, LeftPaddleSpeedIncrement = 0.5f, RightPaddleSpeedIncrement = 0.5f;
        Vector2 BallPosition;
        bool GameActive = false;

        void DrawLine(float x1, float y1, float x2, float y2)
        {
            GL.Begin(BeginMode.Lines);
            GL.Vertex2(x1, y1);
            GL.Vertex2(x2, y2);
            GL.End();
        }
        void DrawLine(Vector2 one, Vector2 two)
        {
            GL.Begin(BeginMode.Lines);
            GL.Vertex2(one.X, one.Y);
            GL.Vertex2(two.X, two.Y);
            GL.End();
        }
        void MovePaddleLeft(int direction) // positive direction to move down, negative to move up
        {
            if (CurrentPage == 1 || CurrentPage == 2)
            {
                RightPaddleSpeed = CurrentPage == 2 ? LeftPaddleSpeed : RightPaddleSpeed;
                if (((direction < 0 && PaddleLeft.Top > 20) || (direction >= 0 && PaddleLeft.Bottom < ClientRectangle.Height - 20)) && GameActive)
                    PaddleLeft = new RectangleF(40, PaddleLeft.Y + ((direction >= 0) ? LeftPaddleSpeed : LeftPaddleSpeed * -1), 20, 160);
            }
        }
        void MovePaddleRight(int direction)
        {
            if (CurrentPage == 1 || CurrentPage == 2)
            {
                RightPaddleSpeed = CurrentPage == 2 ? LeftPaddleSpeed : RightPaddleSpeed;
                if (((direction < 0 && PaddleRight.Top > 20) || (direction >= 0 && PaddleRight.Bottom < ClientRectangle.Height - 20)) && GameActive)
                    PaddleRight = new RectangleF(ClientRectangle.Width - 60, PaddleRight.Y + ((direction >= 0) ? RightPaddleSpeed : RightPaddleSpeed * -1), 20, 160);
            }
        }
        void DrawLeftPaddle()
        {
            GL.Begin(BeginMode.Quads);
            GL.Vertex2(PaddleLeft.Left, PaddleLeft.Top);
            GL.Vertex2(PaddleLeft.Left, PaddleLeft.Bottom);
            GL.Vertex2(PaddleLeft.Right, PaddleLeft.Bottom);
            GL.Vertex2(PaddleLeft.Right, PaddleLeft.Top);
            GL.End();
        }
        void DrawRightPaddle()
        {
            GL.Begin(BeginMode.Quads);
            GL.Vertex2(PaddleRight.Left, PaddleRight.Top);
            GL.Vertex2(PaddleRight.Left, PaddleRight.Bottom);
            GL.Vertex2(PaddleRight.Right, PaddleRight.Bottom);
            GL.Vertex2(PaddleRight.Right, PaddleRight.Top);
            GL.End();
        }
        void DrawBall()
        {
            GL.Begin(BeginMode.TriangleFan);
            for (int i = 0; i < 360; i++)
            {
                double degInRad = i * 3.1416 / 180;
                GL.Vertex2(BallPosition.X + Math.Cos(degInRad) * BallRadius, BallPosition.Y + Math.Sin(degInRad) * BallRadius);
            }
            GL.End();
        }
        bool DontMoveBall = false;
        int MoveBallCounter = 0;
        void MoveBall()
        {
            if (GameActive)
            {
                // 45 frames = 750 ms
                if (DontMoveBall && MoveBallCounter < 45) { MoveBallCounter++; return; }
                DontMoveBall = false;
                if (!SpeedTimer.Enabled) SpeedTimer.Start();
                float BallX = BallPosition.X + ((BallXDirection >= 0) ? BallRadius : BallRadius * -1), BallY = BallPosition.Y + ((BallYDirection >= 0) ? BallRadius : BallRadius * -1), BorderLeft = 20, BorderRight = ClientRectangle.Width - 20, BorderTop = 20, BorderBottom = ClientRectangle.Height - 20;
                if ((BallX >= PaddleLeft.Left && BallX <= PaddleLeft.Right && BallY > PaddleLeft.Top && BallY < PaddleLeft.Bottom) || (BallX >= PaddleRight.Left && BallX <= PaddleRight.Right && BallY > PaddleRight.Top && BallY < PaddleRight.Bottom))
                {
                    BallXDirection = (float)Math.Round((double)BallXDirection);
                    BallXDirection *= (float)(Random.Next(8, 12) * -0.1);
                }
                if (BallX <= BorderLeft || BallX >= BorderRight)
                {
                    if (BallX <= BorderLeft) PointsRight++;
                    else PointsLeft++;
                    ResetGame(true);
                    DontMoveBall = true;
                }
                if (BallY <= BorderTop || BallY >= BorderBottom)
                {
                    BallYDirection = (float)Math.Round((double)BallYDirection);
                    BallYDirection *= (float)(Random.Next(8, 12) * -0.1);
                }
                if (!DontMoveBall)
                {
                    BallPosition.X += BallSpeed * BallXDirection;
                    BallPosition.Y += BallSpeed * BallYDirection;
                }
                if (CurrentPage == 1)
                {
                    if (BallY < PaddleRight.Top + (PaddleRight.Height / 2)) MovePaddleRight(-1);
                    if (BallY > PaddleRight.Bottom - (PaddleRight.Height / 2)) MovePaddleRight(1);
                }
            }
        }
        void ResetGame(bool inGame = false)
        {
            if (!inGame)
            {
                GameActive = false;
                PointsLeft = 0;
                PointsRight = 0;
                PaddleLeft = new RectangleF(40, (ClientRectangle.Height / 2) - 80, 20, 160);
                PaddleRight = new RectangleF(ClientRectangle.Width - 60, (ClientRectangle.Height / 2) - 80, 20, 160);
            }
            BallXDirection = Random.Next(-5, 6) > 0 ? 1 : -1;
            BallYDirection = Random.Next(-5, 6) > 0 ? 1 : -1;
            BallPosition = new Vector2(ClientRectangle.Width / 2, ClientRectangle.Height / 2);
            BallSpeed = InitialBallSpeed;
            if (SpeedTimer.Enabled)
            {
                SpeedTimer.Stop();
                BallSpeed = InitialBallSpeed;
                LeftPaddleSpeed = InitialLeftPaddleSpeed;
                RightPaddleSpeed = InitialRightPaddleSpeed;
            }
        }

        Random Random = new Random();

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Modelview);
            GL.LoadIdentity();

            if (CurrentPage == 0)
            {
                font = new Font(GetFontFamily("Bebas") ?? FontFamily.GenericMonospace, 30.0f);
                TextPrinter.Begin();
                RectangleSpace = new RectangleF(((ClientRectangle.Width / 2)) - 300, ((ClientRectangle.Height / 2)) - 200, 600, 100);
                TextPrinter.Print("Pong", font, Color.Black, RectangleSpace, TextPrinterOptions.Default, TextAlignment.Center);
                font = new Font(GetFontFamily("Bebas") ?? FontFamily.GenericMonospace, 20.0f);
                RectangleSpace = new RectangleF((ClientRectangle.Width / 2) - 150, ClientRectangle.Height / 2 - 150, 300, 100);
                TextPrinter.Print("By Jonny Li", font, SystemColors.ControlDarkDark, RectangleSpace, TextPrinterOptions.Default, TextAlignment.Center);
                var itemColor = Color.Navy;
                int temp = 0;
                foreach (string item in MenuItems)
                {
                    RectangleSpace = new RectangleF((ClientRectangle.Width / 2) - 150, (ClientRectangle.Height / 2) + temp, 300, 40);
                    if ((Mouse.X > RectangleSpace.X && Mouse.X < RectangleSpace.X + RectangleSpace.Width) && (Mouse.Y > RectangleSpace.Y && Mouse.Y < RectangleSpace.Y + RectangleSpace.Height))
                    {
                        itemColor = Color.Turquoise;
                        HoveredItem = MenuItems.IndexOf(item) + 1;
                    }
                    else { itemColor = Color.Navy; }
                    TextPrinter.Print(item, font, itemColor, RectangleSpace, TextPrinterOptions.Default, TextAlignment.Center);
                    temp += 40;
                    itemColor = Color.Navy;
                }
                TextPrinter.End();
            }
            else if (CurrentPage == 1 || CurrentPage == 2)
            {
                if (!GameActive)
                {
                    font = new Font(FontFamily.GenericMonospace, 12.0f);
                    RectangleSpace = new RectangleF((ClientRectangle.Width / 2) - 200, (ClientRectangle.Height / 2) - 200, 400, 40);
                    TextPrinter.Begin();
                    TextPrinter.Print("Hit SPACE whenver you are ready to start. Hit SPACE again to pause.", font, Color.Purple, RectangleSpace, TextPrinterOptions.Default, TextAlignment.Center);
                    RectangleSpace = new RectangleF((ClientRectangle.Width / 2) - 200, (ClientRectangle.Height / 2) - 150, 400, 40);
                    TextPrinter.Print("Press ESCAPE to return to the main menu.", font, Color.Purple, RectangleSpace, TextPrinterOptions.Default, TextAlignment.Center);
                    TextPrinter.End();
                }
                GL.Color3(0, 0, 0);
                for (int i = 0; i < Border.Count; i++) DrawLine(Border[i], (i == Border.Count - 1) ? Border[0] : Border[i + 1]);
                GL.Color3(SystemColors.HotTrack);
                DrawLeftPaddle();
                DrawRightPaddle();
                GL.Color3(SystemColors.Highlight);
                DrawBall();
                TextPrinter.Begin();
                font = new Font(GetFontFamily("Bebas") ?? FontFamily.GenericMonospace, 50.0f);
                RectangleSpace = new RectangleF(200, (ClientRectangle.Height / 2) - 50, 50, 100);
                TextPrinter.Print(PointsLeft.ToString(), font, SystemColors.ControlDarkDark, RectangleSpace, TextPrinterOptions.Default, TextAlignment.Center);
                RectangleSpace = new RectangleF((ClientRectangle.Width - (200 + PaddleRight.Width)), (ClientRectangle.Height / 2) - 50, 50, 100);
                TextPrinter.Print(PointsRight.ToString(), font, SystemColors.ControlDarkDark, RectangleSpace, TextPrinterOptions.Default, TextAlignment.Center);
                TextPrinter.End();
            }
            else if (CurrentPage == 3)
            {
                font = new Font(GetFontFamily("Bebas") ?? FontFamily.GenericMonospace, 30.0f);
                TextPrinter.Begin();
                RectangleSpace = new RectangleF(((ClientRectangle.Width / 2)) - 300, ((ClientRectangle.Height / 2)) - 200, 600, 100);
                TextPrinter.Print("Instructions", font, Color.Black, RectangleSpace, TextPrinterOptions.Default, TextAlignment.Center);
                font = new Font(FontFamily.GenericMonospace, 11.0f);
                var lines = new List<string>() { "Welcome to Pong!", "To play the game, simply use the W and S keys (for Player 1, left side) to move the paddle up and down.", "If you are in Player vs Player mode, Player 2 can use the UP and DOWN arrow keys to manipulate his paddle (right side).", "Lastly, use your mouse to navigate around the menus and pages.", "You can press the ESCAPE key to return back to the start menu anytime you want." };
                int temp = -50;
                foreach (string line in lines)
                {
                    RectangleSpace = new RectangleF((ClientRectangle.Width / 2) - 300, (ClientRectangle.Height / 2) + temp, 600, 40);
                    TextPrinter.Print(line, font, Color.Purple, RectangleSpace, TextPrinterOptions.Default, TextAlignment.Center);
                    temp += 40;
                }
                RectangleSpace = new RectangleF(0, 0, 0, 0);
                TextPrinter.End();
            }
            else CurrentPage = 0;

            SwapBuffers();
        }

        bool IsHoveringOnMenuItem()
        {
            int temp = 0;
            foreach (string item in MenuItems)
            {
                RectangleSpace = new RectangleF((ClientRectangle.Width / 2) - 150, (ClientRectangle.Height / 2) + temp, 300, 40);
                if ((Mouse.X > RectangleSpace.X && Mouse.X < RectangleSpace.X + RectangleSpace.Width) && (Mouse.Y > RectangleSpace.Y && Mouse.Y < RectangleSpace.Y + RectangleSpace.Height))
                    return true;
                temp += 40;
            }
            return false;
        }

        static void Main(string[] args)
        {
            using (Window window = new Window())
                window.Run(60.0f);
        }
    }
}
