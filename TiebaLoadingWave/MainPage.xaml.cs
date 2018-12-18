using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Win2dDemos
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Unloaded += MainPage_Unloaded;
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            this.canvas.RemoveFromVisualTree();
            this.canvas = null;
        }
        Random rnd = new Random();
        private Vector2 RndPosition()
        {
            double x = rnd.NextDouble() * 500f;
            double y = rnd.NextDouble() * 500f;
            return new Vector2((float)x, (float)y);
        }

        private float RndRadius()
        {
            return (float)rnd.NextDouble() * 150f;
        }

        private byte RndByte()
        {
            return (byte)rnd.Next(256);
        }
        private void canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            
                
        }

        int offsetX = 0;
        int percent = 0;

        int rate = 0;

        int RadiusValue = 100;
        private void canvas_DrawAnimated(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            CreateLoadingWave(sender, args, new Vector2(0, 0), Color.FromArgb(255, 195, 71, 59));
            CreateLoadingWaveText(sender, args, new Vector2(300, 0), Color.FromArgb(255, 99, 149, 176)) ;
        }
        public void CreateLoadingWave(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args, Vector2 position,Color color)
        {
            if (rate >= 10)
            {
                percent++;
                rate = 0;
            }
            if (percent > 100)
            {
                percent = 0;
            }

            CanvasTextLayout textLayout = new CanvasTextLayout(sender, $"{percent}", new CanvasTextFormat() { FontSize = RadiusValue }, RadiusValue * 2, RadiusValue * 2);

            CanvasGeometry orignalText = CanvasGeometry.CreateText(textLayout);
            var rectText = orignalText.ComputeBounds();
            var textOffsetX = (RadiusValue * 2 - textLayout.LayoutBoundsIncludingTrailingWhitespace.Width) / 2;
            var textOffsetY = (RadiusValue * 2 - textLayout.LayoutBoundsIncludingTrailingWhitespace.Height) / 2;

            orignalText = orignalText.Transform(Matrix3x2.CreateTranslation((float)textOffsetX + position.X, (float)textOffsetY + position.Y));

            CanvasPathBuilder builder = new CanvasPathBuilder(sender);

            var offsetY = 2 * rate / 10 + percent * 2;
            builder.BeginFigure(0 + offsetX + position.X, RadiusValue * 2 - offsetY + position.Y);

            builder.AddCubicBezier(new Vector2(RadiusValue * 1 + offsetX + position.X, RadiusValue * 2 + RadiusValue / 3 - offsetY + position.Y), new Vector2(RadiusValue * 1 + offsetX + position.X, RadiusValue * 2 - RadiusValue / 3 - offsetY + position.Y), new Vector2(RadiusValue * 2 + offsetX + position.X, RadiusValue * 2 - offsetY + position.Y));

            builder.AddCubicBezier(new Vector2(RadiusValue * 3 + offsetX + position.X, RadiusValue * 2 + RadiusValue / 3 - offsetY + position.Y), new Vector2(RadiusValue * 3 + offsetX + position.X, RadiusValue * 2 - RadiusValue / 3 - offsetY + position.Y), new Vector2(RadiusValue * 4 + offsetX + position.X, RadiusValue * 2 - offsetY + position.Y));

            builder.AddLine(RadiusValue * 4 + offsetX + position.X, RadiusValue * 4 + position.Y);
            builder.AddLine(0 + offsetX + position.X, RadiusValue * 4 + position.Y);

            builder.EndFigure(CanvasFigureLoop.Closed);

            var wavePath = CanvasGeometry.CreatePath(builder);
            var circlePath = CanvasGeometry.CreateCircle(sender, new Vector2(RadiusValue, RadiusValue), RadiusValue);

            var backgroundPath = circlePath.CombineWith(wavePath, Matrix3x2.Identity, CanvasGeometryCombine.Intersect);

            var topText = orignalText.CombineWith(backgroundPath, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
            var drawnText = orignalText.CombineWith(backgroundPath, Matrix3x2.Identity, CanvasGeometryCombine.Intersect);


             args.DrawingSession.FillGeometry(backgroundPath,position, color);
            args.DrawingSession.FillGeometry(topText, color);
            args.DrawingSession.FillGeometry(drawnText, Colors.White);

            var borderCircle = CanvasGeometry.CreateCircle(sender, new Vector2(RadiusValue, RadiusValue), RadiusValue - 1);
            args.DrawingSession.DrawGeometry(borderCircle, position, color);

            offsetX--;
            if (offsetX <= -RadiusValue * 2)
            {
                offsetX = 0;
            }
            rate++;

        }

        public void CreateLoadingWaveText(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args,Vector2 position,Color color)
        {
            if (rate >= 10)
            {
                percent++;
                rate = 0;
            }
            if (percent > 100)
            {
                percent = 0;
            }

            CanvasTextLayout textLayout = new CanvasTextLayout(sender, $"钴{Environment.NewLine}藍", new CanvasTextFormat() { FontSize = RadiusValue/2 }, RadiusValue*2 , RadiusValue*2);
            textLayout.SetFontFamily(0, 4, "华文楷体");

            CanvasGeometry orignalText = CanvasGeometry.CreateText(textLayout);
            var rectText = orignalText.ComputeBounds();
            var textOffsetX = (RadiusValue * 2 - textLayout.LayoutBoundsIncludingTrailingWhitespace.Width) / 2;
            var textOffsetY = (RadiusValue * 2 - textLayout.LayoutBoundsIncludingTrailingWhitespace.Height) / 2;

            orignalText = orignalText.Transform(Matrix3x2.CreateTranslation((float)textOffsetX+ position.X, (float)textOffsetY+ position.Y));

            CanvasPathBuilder builder = new CanvasPathBuilder(sender);

            var offsetY = 2 * rate / 10 + percent * 2;
            builder.BeginFigure(0 + offsetX+ position.X, RadiusValue * 2 - offsetY+ position.Y);

            builder.AddCubicBezier(new Vector2(RadiusValue * 1 + offsetX + position.X, RadiusValue * 2 + RadiusValue / 3 - offsetY + position.Y), new Vector2(RadiusValue * 1 + offsetX + position.X, RadiusValue * 2 - RadiusValue / 3 - offsetY + position.Y), new Vector2(RadiusValue * 2 + offsetX + position.X, RadiusValue * 2 - offsetY + position.Y));

            builder.AddCubicBezier(new Vector2(RadiusValue * 3 + offsetX + position.X, RadiusValue * 2 + RadiusValue / 3 - offsetY + position.Y), new Vector2(RadiusValue * 3 + offsetX + position.X, RadiusValue * 2 - RadiusValue / 3 - offsetY + position.Y), new Vector2(RadiusValue * 4 + offsetX + position.X, RadiusValue * 2 - offsetY + position.Y));

            builder.AddLine(RadiusValue * 4 + offsetX + position.X, RadiusValue * 4 + position.Y);
            builder.AddLine(0 + offsetX + position.X, RadiusValue * 4 + position.Y);

            builder.EndFigure(CanvasFigureLoop.Closed);

            var wavePath = CanvasGeometry.CreatePath(builder);
            var circlePath = CanvasGeometry.CreateCircle(sender, new Vector2(RadiusValue, RadiusValue), RadiusValue);
            circlePath = circlePath.Transform(Matrix3x2.CreateTranslation(position));
            var backgroundPath = circlePath.CombineWith(wavePath, Matrix3x2.Identity, CanvasGeometryCombine.Intersect);

            var topText = orignalText.CombineWith(backgroundPath, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
            var drawnText = orignalText.CombineWith(backgroundPath, Matrix3x2.Identity, CanvasGeometryCombine.Intersect);


            args.DrawingSession.FillGeometry(backgroundPath, Color.FromArgb(255, 99, 149, 176));
            args.DrawingSession.FillGeometry(topText, Color.FromArgb(255, 99, 149, 176));
            args.DrawingSession.FillGeometry(drawnText, Colors.White);

            var borderCircle = CanvasGeometry.CreateCircle(sender, new Vector2(RadiusValue, RadiusValue), RadiusValue - 1);
            args.DrawingSession.DrawGeometry(borderCircle, position, Color.FromArgb(255, 99, 149, 176), 2);

            offsetX--;
            if (offsetX <= -RadiusValue*2)
            {
                offsetX = 0;
            }
            rate++;

        }

        private void canvas_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
        }
    }
}
