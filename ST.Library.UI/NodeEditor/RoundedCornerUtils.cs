using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;

namespace ST.Library.UI.NodeEditor
{
    public class RoundedCornerUtils
    {
        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        public static GraphicsPath RoundedRectTop(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom border
            path.AddLine(new PointF(bounds.Right, bounds.Bottom), new PointF(bounds.Left, bounds.Bottom));

            path.CloseFigure();
            return path;
        }

        public static GraphicsPath RoundedRectBottom(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top border.
            path.AddLine(new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right, bounds.Top));

            arc.X = bounds.Right - diameter;

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        public static void DrawRoundedRectangle(Graphics graphics, Pen pen, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (pen == null)
                throw new ArgumentNullException("pen");

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
            {
                var oldSmoothingMode = graphics.SmoothingMode;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.DrawPath(pen, path);
                graphics.SmoothingMode = oldSmoothingMode;
            }
        }

        public static void FillRoundedRectangle(Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (brush == null)
                throw new ArgumentNullException("brush");

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
            {
                var oldSmoothingMode = graphics.SmoothingMode;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillPath(brush, path);
                graphics.SmoothingMode = oldSmoothingMode;
            }
        }

        public static void DrawRoundedRectangleTop(Graphics graphics, Pen pen, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (pen == null)
                throw new ArgumentNullException("pen");

            using (GraphicsPath path = RoundedRectTop(bounds, cornerRadius))
            {
                var oldSmoothingMode = graphics.SmoothingMode;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.DrawPath(pen, path);
                graphics.SmoothingMode = oldSmoothingMode;
            }
        }

        public static void FillRoundedRectangleTop(Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (brush == null)
                throw new ArgumentNullException("brush");

            using (GraphicsPath path = RoundedRectTop(bounds, cornerRadius))
            {
                var oldSmoothingMode = graphics.SmoothingMode;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillPath(brush, path);
                graphics.SmoothingMode = oldSmoothingMode;
            }
        }

        public static void DrawRoundedRectangleBottom(Graphics graphics, Pen pen, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (pen == null)
                throw new ArgumentNullException("pen");

            using (GraphicsPath path = RoundedRectBottom(bounds, cornerRadius))
            {
                var oldSmoothingMode = graphics.SmoothingMode;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.DrawPath(pen, path);
                graphics.SmoothingMode = oldSmoothingMode;
            }
        }

        public static void FillRoundedRectangleBottom(Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (brush == null)
                throw new ArgumentNullException("brush");

            using (GraphicsPath path = RoundedRectBottom(bounds, cornerRadius))
            {
                var oldSmoothingMode = graphics.SmoothingMode;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillPath(brush, path);
                graphics.SmoothingMode = oldSmoothingMode;
            }
        }
    }
}
