using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.UI;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI.Core;

namespace InertialSensor.Desktop
{
  class ChartRenderer
  {
    public void RenderAxes(CanvasAnimatedControl canvas, CanvasAnimatedDrawEventArgs args)
    {
      var width = Constants.ChartWidth;
      var height = Constants.ChartHeight;
      var midWidth = (float)(width * .5);
      var midHeight = (float)(height * .5);

      using (var cpb = new CanvasPathBuilder(args.DrawingSession))
      {
        // Horizontal line
        cpb.BeginFigure(new Vector2(0, midHeight));
        cpb.AddLine(new Vector2(width, midHeight));
        cpb.EndFigure(CanvasFigureLoop.Open);

        // Horizontal line arrow
        cpb.BeginFigure(new Vector2(width - 10, midHeight - 3));
        cpb.AddLine(new Vector2(width, midHeight));
        cpb.AddLine(new Vector2(width - 10, midHeight + 3));
        cpb.EndFigure(CanvasFigureLoop.Open);

        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), Colors.Gray, 1);
      }

      args.DrawingSession.DrawText("0", 5, midHeight - 30, Colors.Gray);
      args.DrawingSession.DrawText(width.ToString(), width - 50, midHeight - 30, Colors.Gray);

      using (var cpb = new CanvasPathBuilder(args.DrawingSession))
      {
        // Vertical line
        cpb.BeginFigure(new Vector2(midWidth, 0));
        cpb.AddLine(new Vector2(midWidth, height));
        cpb.EndFigure(CanvasFigureLoop.Open);

        // Vertical line arrow
        cpb.BeginFigure(new Vector2(midWidth - 3, 10));
        cpb.AddLine(new Vector2(midWidth, 0));
        cpb.AddLine(new Vector2(midWidth + 3, 10));
        cpb.EndFigure(CanvasFigureLoop.Open);

        args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), Colors.Gray, 1);
      }

      args.DrawingSession.DrawText("0", midWidth + 5, height - 30, Colors.Gray);
      args.DrawingSession.DrawText("1", midWidth + 5, 5, Colors.Gray);
    }

    public void RenderData(CanvasAnimatedControl canvas, CanvasAnimatedDrawEventArgs args, Color color, float thickness, List<XYZ> data)
    {
      using (var cpb = new CanvasPathBuilder(args.DrawingSession))
      {
        using (var dataSet2 = new CanvasPathBuilder(args.DrawingSession))
        {
          using (var dataSet3 = new CanvasPathBuilder(args.DrawingSession))
          {
            XYZ firstVal = data[0];
            cpb.BeginFigure(new Vector2(0, (float)((firstVal.X + 32) * 10)));
            dataSet2.BeginFigure(new Vector2(0, (float)((firstVal.Y + 32) * 10)));
            dataSet3.BeginFigure(new Vector2(0, (float)((firstVal.Z + 32) * 10)));
            int width = data.Count < Constants.ChartWidth ? data.Count : Constants.ChartWidth;
            for (int i = 0; i < width; i++)
            {
              XYZ val = data[i];
              cpb.AddLine(new Vector2(i, (float)(((val.X * -1) + 32) * 10)));
              dataSet2.AddLine(new Vector2(i, (float)(((val.Y * -1) + 32) * 10)));
              dataSet3.AddLine(new Vector2(i, (float)(((val.Z * -1) + 32) * 10)));
            }
            cpb.EndFigure(CanvasFigureLoop.Open);
            dataSet2.EndFigure(CanvasFigureLoop.Open);
            dataSet3.EndFigure(CanvasFigureLoop.Open);
            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), Colors.Black, thickness);
            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(dataSet2), Colors.Fuchsia, thickness);
            args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(dataSet3), Colors.PaleVioletRed, thickness);
          }
        }
      }
    }
  }
}
