using System.Text;
using PixelTerminalUI.Contracts.Common;

namespace PixelTerminalUI.Engine.Tests.Rendering.WidgetRendering;

public abstract class BaseWidgetRendererTests
{
    protected static string ConvertFlatBufferToVisualString(Pixel[] buffer, int width, int height)
    {
        StringBuilder stringBuilder = new();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                stringBuilder.Append(buffer[y * width + x].Symbol == '\0' ? ' ' : buffer[y * width + x].Symbol);
            }

            if (y < height - 1)
            {
                stringBuilder.Append(Environment.NewLine);
            }
        }

        return stringBuilder.ToString();
    }
}