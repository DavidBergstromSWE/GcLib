using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsDemoApp.Controls;

/// <summary>
/// Customized group box control, allows change of border and text label colors.
/// </summary>
public partial class BorderedGroupBox : GroupBox
{
    public BorderedGroupBox() : base() { }

    private Color borderColor = Color.Black;

    [DefaultValue(typeof(Color), "Black")]
    public Color BorderColor
    {
        get { return borderColor; }
        set { borderColor = value; Invalidate(); }
    }

    private Color textColor = Color.Black;

    [DefaultValue(typeof(Color), "Black")]
    public Color TextColor
    {
        get { return textColor; }
        set { textColor = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        TextFormatFlags flags = TextFormatFlags.PreserveGraphicsTranslateTransform | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak;
        Color titleColor = TextColor;
        if (!ShowKeyboardCues)
            flags |= TextFormatFlags.HidePrefix;
        if (RightToLeft == RightToLeft.Yes)
            flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;
        if (!Enabled)
            titleColor = SystemColors.GrayText;
        DrawUnthemedGroupBoxWithText(e.Graphics, new Rectangle(0, 0, Width, Height), Text, Font, titleColor, flags);
        RaisePaintEvent(this, e);
    }

    private void DrawUnthemedGroupBoxWithText(Graphics g, Rectangle bounds, string groupBoxText, Font font, Color titleColor, TextFormatFlags flags)
    {
        Rectangle rectangle = bounds;
        rectangle.Width -= 8;
        Size size = TextRenderer.MeasureText(g, groupBoxText, font, new Size(rectangle.Width, rectangle.Height), flags);
        rectangle.Width = size.Width;
        rectangle.Height = size.Height;

        if ((flags & TextFormatFlags.Right) == TextFormatFlags.Right)
            rectangle.X = (bounds.Right - rectangle.Width) - 8;
        else
            rectangle.X += 8;

        TextRenderer.DrawText(g, groupBoxText, font, rectangle, titleColor, flags);

        if (rectangle.Width > 0)
            rectangle.Inflate(2, 0);

        using var pen = new Pen(BorderColor);
        int num = bounds.Top + (font.Height / 2);
        g.DrawLine(pen, bounds.Left, num - 1, bounds.Left, bounds.Height - 2);
        g.DrawLine(pen, bounds.Left, bounds.Height - 2, bounds.Width - 1, bounds.Height - 2);
        g.DrawLine(pen, bounds.Left, num - 1, rectangle.X - 3, num - 1);
        g.DrawLine(pen, rectangle.X + rectangle.Width + 2, num - 1, bounds.Width - 2, num - 1);
        g.DrawLine(pen, bounds.Width - 2, num - 1, bounds.Width - 2, bounds.Height - 2);
    }
}
