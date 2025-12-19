using SkiaSharp;
using System.IO;

namespace TamagotchiBot.Services.Helpers
{
    public static class HangmanImageGenerator
    {
        public static Stream GenerateImage(int wrongGuesses, string wordToDisplay)
        {
            int width = 400;
            int height = 400;

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            using (var paint = new SKPaint
            {
                Color = SKColors.Black,
                StrokeWidth = 4,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            })
            {
                // Draw Gallows
                // Base
                canvas.DrawLine(50, 350, 250, 350, paint);
                // Pole
                canvas.DrawLine(150, 350, 150, 50, paint);
                // Top Bar
                canvas.DrawLine(150, 50, 300, 50, paint);
                // Rope
                canvas.DrawLine(300, 50, 300, 100, paint);

                // Draw Man
                if (wrongGuesses >= 1) // Head
                    canvas.DrawCircle(300, 130, 30, paint);

                if (wrongGuesses >= 2) // Body
                    canvas.DrawLine(300, 160, 300, 260, paint);

                if (wrongGuesses >= 3) // Left Arm
                    canvas.DrawLine(300, 180, 250, 230, paint);

                if (wrongGuesses >= 4) // Right Arm
                    canvas.DrawLine(300, 180, 350, 230, paint);

                if (wrongGuesses >= 5) // Left Leg
                    canvas.DrawLine(300, 260, 260, 330, paint);

                if (wrongGuesses >= 6) // Right Leg
                    canvas.DrawLine(300, 260, 340, 330, paint);

                if (wrongGuesses >= 7) // Eyes (X X)  - Optional extra detail for loss
                {
                    using var eyePaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 3, IsAntialias = true };
                    // Left Eye
                    canvas.DrawLine(285, 120, 295, 130, eyePaint);
                    canvas.DrawLine(295, 120, 285, 130, eyePaint);
                    // Right Eye
                    canvas.DrawLine(305, 120, 315, 130, eyePaint);
                    canvas.DrawLine(315, 120, 305, 130, eyePaint);
                }
            }

            // Draw Word
            using (var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 40))
            using (var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            })
            {
                // Center the text
                float textWidth = font.MeasureText(wordToDisplay);
                float x = (width - textWidth) / 2;
                float y = 390;

                canvas.DrawText(wordToDisplay, x, y, font, textPaint);
            }

            // Clean up and return stream
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var ms = new MemoryStream();
            data.SaveTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
