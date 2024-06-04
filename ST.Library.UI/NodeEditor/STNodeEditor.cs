using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using System.IO.Compression;
/*
MIT License

Copyright (c) 2021 DebugST@crystal_lz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
/*
 * create: 2020-12-08
 * modify: 2021-04-12
 * Author: Crystal_lz
 * blog: http://st233.com
 * Gitee: https://gitee.com/DebugST
 * Github: https://github.com/DebugST
 */
namespace ST.Library.UI.NodeEditor
{
    public class STNodeEditor : Control
    {
        private const UInt32 WM_MOUSEHWHEEL = 0x020E;
        protected static readonly Type m_type_node = typeof(STNode);

        #region protected enum,struct --------------------------------------------------------------------------------------

        protected enum CanvasAction     //The current mouse movement operation indicates which of the following actions is performed
        {
            None,                       //None
            MoveNode,                   //Moving Node
            ConnectOption,              //Connecting Option
            SelectRectangle,            //Selecting rectangular area
            DrawMarkDetails             //Drawing marker information details
        }

        protected struct MagnetInfo
        {
            public bool XMatched;       //Is there a magnet matching on the X axis?
            public bool YMatched;
            public int X;               //Match the number on the X axis
            public int Y;
            public int OffsetX;         //The relative offset of the current node's X position and the matched X
            public int OffsetY;
        }

        #endregion

        #region Properties ------------------------------------------------------------------------------------------------------

        private float _CanvasOffsetX;
        /// <summary>
        /// Get the offset position of the canvas origin relative to the X direction of the control.
        /// </summary>
        [Browsable(false)]
        public float CanvasOffsetX {
            get { return _CanvasOffsetX; }
        }

        private float _CanvasOffsetY;
        /// <summary>
        /// Get the offset position of the canvas origin relative to the Y direction of the control.
        /// </summary>
        [Browsable(false)]
        public float CanvasOffsetY {
            get { return _CanvasOffsetY; }
        }

        private PointF _CanvasOffset;
        /// <summary>
        /// Get the offset position of the canvas origin relative to the control.
        /// </summary>
        [Browsable(false)]
        public PointF CanvasOffset {
            get {
                _CanvasOffset.X = _CanvasOffsetX;
                _CanvasOffset.Y = _CanvasOffsetY;
                return _CanvasOffset;
            }
        }

        private Rectangle _CanvasValidBounds;
        /// <summary>
        /// Get the effective area in the canvas that is used.
        /// </summary>
        [Browsable(false)]
        public Rectangle CanvasValidBounds {
            get { return _CanvasValidBounds; }
        }

        private float _CanvasScale = 1;
        /// <summary>
        /// Get the zoom ratio of the canvas.
        /// </summary>
        [Browsable(false)]
        public float CanvasScale {
            get { return _CanvasScale; }
        }

        private float _Curvature = 0.3F;
        /// <summary>
        /// Get or set the curvature of the lines between Options.
        /// </summary>
        [Browsable(false)]
        public float Curvature {
            get { return _Curvature; }
            set {
                if (value < 0) value = 0;
                if (value > 1) value = 1;
                _Curvature = value;
                if (m_dic_gp_info.Count != 0) this.BuildLinePath();
            }
        }

        private bool _ShowMagnet = true;
        /// <summary>
        /// Gets or sets whether to enable the magnet effect when moving the Node in the canvas.
        /// </summary>
        [Description("Gets or sets whether to enable the magnet effect when moving the Node in the canvas."), DefaultValue(true)]
        public bool ShowMagnet {
            get { return _ShowMagnet; }
            set { _ShowMagnet = value; }
        }

        private bool _ShowBorder = true;
        /// <summary>
        /// Gets or sets whether to display the Node border in the moving canvas.
        /// </summary>
        [Description("Gets or sets whether to display the Node border in the moving canvas."), DefaultValue(true)]
        public bool ShowBorder {
            get { return _ShowBorder; }
            set {
                _ShowBorder = value;
                this.Invalidate();
            }
        }

        private bool _ShowGrid = true;
        /// <summary>
        /// Gets or sets whether to draw background grid lines in the canvas.
        /// </summary>
        [Description("Gets or sets whether to draw background grid lines in the canvas."), DefaultValue(true)]
        public bool ShowGrid {
            get { return _ShowGrid; }
            set {
                _ShowGrid = value;
                this.Invalidate();
            }
        }

        private bool _ShowLocation = true;
        /// <summary>
        /// Get or set whether to display Node position information beyond the angle of view at the edge of the canvas.
        /// </summary>
        [Description("Get or set whether to display Node position information beyond the angle of view at the edge of the canvas."), DefaultValue(true)]
        public bool ShowLocation {
            get { return _ShowLocation; }
            set {
                _ShowLocation = value;
                this.Invalidate();
            }
        }

        private STNodeCollection _Nodes;
        /// <summary>
        /// Get the Node collection in the canvas.
        /// </summary>
        [Browsable(false)]
        public STNodeCollection Nodes {
            get {
                return _Nodes;
            }
        }

        private STNode _ActiveNode;
        /// <summary>
        /// Get the selected active Node in the current canvas.
        /// </summary>
        [Browsable(false)]
        public STNode ActiveNode {
            get { return _ActiveNode; }
            //set {
            //    if (value == _ActiveSelectedNode) return;
            //    if (_ActiveSelectedNode != null) _ActiveSelectedNode.OnLostFocus(EventArgs.Empty);
            //    _ActiveSelectedNode = value;
            //    _ActiveSelectedNode.IsActive = true;
            //    this.Invalidate();
            //    this.OnSelectedChanged(EventArgs.Empty);
            //}
        }

        private STNode _HoverNode;
        /// <summary>
        /// Get the Node where the mouse is hovering in the current canvas.
        /// </summary>
        [Browsable(false)]
        public STNode HoverNode {
            get { return _HoverNode; }
        }
        //========================================color================================
        private Color _GridColor = Color.Black;
        /// <summary>
        /// Gets or sets the grid line color when drawing the canvas background.
        /// </summary>
        [Description("Gets or sets the grid line color when drawing the canvas background."), DefaultValue(typeof(Color), "Black")]
        public Color GridColor {
            get { return _GridColor; }
            set {
                _GridColor = value;
                this.Invalidate();
            }
        }

        private Color _BorderColor = Color.Black;
        /// <summary>
        /// Get or set the border color of Node in the canvas.
        /// </summary>
        [Description("Get or set the border color of Node in the canvas."), DefaultValue(typeof(Color), "Black")]
        public Color BorderColor {
            get { return _BorderColor; }
            set {
                _BorderColor = value;
                if (m_img_border != null) m_img_border.Dispose();
                m_img_border = this.CreateBorderImage(value);
                this.Invalidate();
            }
        }

        private Color _BorderHoverColor = Color.Gray;
        /// <summary>
        /// Gets or sets the border color of the hovering Node in the canvas.
        /// </summary>
        [Description("Gets or sets the border color of the hovering Node in the canvas."), DefaultValue(typeof(Color), "Gray")]
        public Color BorderHoverColor {
            get { return _BorderHoverColor; }
            set {
                _BorderHoverColor = value;
                if (m_img_border_hover != null) m_img_border_hover.Dispose();
                m_img_border_hover = this.CreateBorderImage(value);
                this.Invalidate();
            }
        }

        private Color _BorderSelectedColor = Color.Orange;
        /// <summary>
        /// Gets or sets the border color of the selected Node in the canvas.
        /// </summary>
        [Description("Gets or sets the border color of the selected Node in the canvas."), DefaultValue(typeof(Color), "Orange")]
        public Color BorderSelectedColor {
            get { return _BorderSelectedColor; }
            set {
                _BorderSelectedColor = value;
                if (m_img_border_selected != null) m_img_border_selected.Dispose();
                m_img_border_selected = this.CreateBorderImage(value);
                this.Invalidate();
            }
        }

        private Color _BorderActiveColor = Color.OrangeRed;
        /// <summary>
        /// Gets or sets the border color of the active Node in the canvas.
        /// </summary>
        [Description("Gets or sets the border color of the active Node in the canvas."), DefaultValue(typeof(Color), "OrangeRed")]
        public Color BorderActiveColor {
            get { return _BorderActiveColor; }
            set {
                _BorderActiveColor = value;
                if (m_img_border_active != null) m_img_border_active.Dispose();
                m_img_border_active = this.CreateBorderImage(value);
                this.Invalidate();
            }
        }

        private Color _MarkForeColor = Color.White;
        /// <summary>
        /// Gets or sets the foreground color used for canvas drawing Node mark details.
        /// </summary>
        [Description("Gets or sets the foreground color used for canvas drawing Node mark details."), DefaultValue(typeof(Color), "White")]
        public Color MarkForeColor {
            get { return _MarkBackColor; }
            set {
                _MarkBackColor = value;
                this.Invalidate();
            }
        }

        private Color _MarkBackColor = Color.FromArgb(180, Color.Black);
        /// <summary>
        /// Gets or sets the background color used for drawing Node mark details on the canvas.
        /// </summary>
        [Description("Gets or sets the background color used for drawing Node mark details on the canvas.")]
        public Color MarkBackColor {
            get { return _MarkBackColor; }
            set {
                _MarkBackColor = value;
                this.Invalidate();
            }
        }

        private Color _MagnetColor = Color.Lime;
        /// <summary>
        /// Get or set the magnet mark color when moving Node in the canvas.
        /// </summary>
        [Description("Get or set the magnet mark color when moving Node in the canvas."), DefaultValue(typeof(Color), "Lime")]
        public Color MagnetColor {
            get { return _MagnetColor; }
            set { _MagnetColor = value; }
        }

        private Color _SelectedRectangleColor = Color.DodgerBlue;
        /// <summary>
        /// Gets or sets the color of the selected rectangular area in the canvas.
        /// </summary>
        [Description("Gets or sets the color of the selected rectangular area in the canvas."), DefaultValue(typeof(Color), "DodgerBlue")]
        public Color SelectedRectangleColor {
            get { return _SelectedRectangleColor; }
            set { _SelectedRectangleColor = value; }
        }

        private Color _HighLineColor = Color.Cyan;
        /// <summary>
        /// Get or set the color of the highlighted line in the canvas.
        /// </summary>
        [Description("Get or set the color of the highlighted line in the canvas."), DefaultValue(typeof(Color), "Cyan")]
        public Color HighLineColor {
            get { return _HighLineColor; }
            set { _HighLineColor = value; }
        }

        private Color _LocationForeColor = Color.Red;
        /// <summary>
        /// Gets or sets the foreground color of the hint area at the edge of the canvas.
        /// </summary>
        [Description("Gets or sets the foreground color of the hint area at the edge of the canvas."), DefaultValue(typeof(Color), "Red")]
        public Color LocationForeColor {
            get { return _LocationForeColor; }
            set {
                _LocationForeColor = value;
                this.Invalidate();
            }
        }

        private Color _LocationBackColor = Color.FromArgb(120, Color.Black);
        /// <summary>
        /// Gets or sets the background color of the edge position prompt area in the canvas.
        /// </summary>
        [Description("Gets or sets the background color of the edge position prompt area in the canvas.")]
        public Color LocationBackColor {
            get { return _LocationBackColor; }
            set {
                _LocationBackColor = value;
                this.Invalidate();
            }
        }

        private Color _UnknownTypeColor = Color.Gray;
        /// <summary>
        /// Gets or sets the color that should be used in the canvas when the Option data type in Node cannot be determined.
        /// </summary>
        [Description("Gets or sets the color that should be used in the canvas when the Option data type in Node cannot be determined."), DefaultValue(typeof(Color), "Gray")]
        public Color UnknownTypeColor {
            get { return _UnknownTypeColor; }
            set {
                _UnknownTypeColor = value;
                this.Invalidate();
            }
        }

        private Dictionary<Type, Color> _TypeColor = new Dictionary<Type, Color>();
        /// <summary>
        /// Get or set the preset color of Option data type in Node in the canvas.
        /// </summary>
        [Browsable(false)]
        public Dictionary<Type, Color> TypeColor {
            get { return _TypeColor; }
        }

        private bool mRequireCtrlForZooming = true;
        public bool RequireCtrlForZooming
        {
            get { return mRequireCtrlForZooming; }
            set { mRequireCtrlForZooming = value; }
        }

        private int mRoundedCornerRadius = -1; // -1 Means no rounded corner rendering.
        public int RoundedCornerRadius
        {
            get { return mRoundedCornerRadius; }
            set { mRoundedCornerRadius = value; }
        }

        private bool mAllowNodeGraphLoops = false;
        public bool AllowNodeGraphLoops
        {
            get { return mAllowNodeGraphLoops; }
            set { mAllowNodeGraphLoops = value; }
        }

        private bool mResetViewWhenEmpty = true;
        public bool ResetViewWhenEmpty
        {
            get { return mResetViewWhenEmpty; }
            set { mResetViewWhenEmpty = value; }
        }

        #endregion

        #region protected properties ----------------------------------------------------------------------------------------
        /// <summary>
        /// The current real-time position of the mouse in the control.
        /// </summary>
        protected Point m_pt_in_control;
        /// <summary>
        /// The current real-time position of the mouse in the canvas.
        /// </summary>
        protected PointF m_pt_in_canvas;
        /// <summary>
        /// The position on the control when the mouse is clicked.
        /// </summary>
        protected Point m_pt_down_in_control;
        /// <summary>
        /// The position in the canvas when the mouse is clicked.
        /// </summary>
        protected PointF m_pt_down_in_canvas;
        /// <summary>
        /// Used to move the canvas when the mouse is clicked, and the coordinate position of the canvas when the mouse is clicked.
        /// </summary>
        protected PointF m_pt_canvas_old;
        /// <summary>
        /// Used to save the starting point coordinates of the Option under the save point during the connection process.
        /// </summary>
        protected Point m_pt_dot_down;
        /// <summary>
        /// Used to save the starting point under the mouse point in the connection process Option When MouseUP determines whether to connect to this node.
        /// </summary>
        protected STNodeOption m_option_down;
        /// <summary>
        /// STNode under the current mouse click.
        /// </summary>
        protected STNode m_node_down;
        /// <summary>
        /// Whether the current mouse is in the control.
        /// </summary>
        protected bool m_mouse_in_control;

        #endregion

        public STNodeEditor() {
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this._Nodes = new STNodeCollection(this);
            this.BackColor = Color.FromArgb(255, 34, 34, 34);
            this.MinimumSize = new Size(100, 100);
            this.Size = new Size(200, 200);
            this.AllowDrop = true;

            m_real_canvas_x = this._CanvasOffsetX = 10;
            m_real_canvas_y = this._CanvasOffsetY = 10;
        }

        #region private fields --------------------------------------------------------------------------------------

        private DrawingTools m_drawing_tools;
        private NodeFindInfo m_find = new NodeFindInfo();
        private MagnetInfo m_mi = new MagnetInfo();

        private RectangleF m_rect_select = new RectangleF();
        //Node border preset pattern
        private Image m_img_border;
        private Image m_img_border_hover;
        private Image m_img_border_selected;
        private Image m_img_border_active;

        private Pen m_pen_border;
        private Pen m_pen_border_hover;
        private Pen m_pen_border_selected;
        private Pen m_pen_border_active;

        //Used for the animation effect when the mouse scrolls or the touchpad moves the canvas. This value is the real coordinate address that needs to be moved to. View->MoveCanvasThread()
        private float m_real_canvas_x;
        private float m_real_canvas_y;
        //Used to save the initial coordinates of the selected node when the mouse is clicked
        private Dictionary<STNode, Point> m_dic_pt_selected = new Dictionary<STNode, Point>();
        //Used for magnet effect When moving nodes, the statistics of non-selected nodes need to participate in the coordinates of the magnet effect. View->BuildMagnetLocation()
        private List<int> m_lst_magnet_x = new List<int>();
        private List<int> m_lst_magnet_y = new List<int>();
        //Used for the magnet effect when moving the node, the active selection node counts the coordinates that need to participate in the magnet effect View->CheckMagnet()
        private List<int> m_lst_magnet_mx = new List<int>();
        private List<int> m_lst_magnet_my = new List<int>();
        //It is used to calculate the time trigger interval during mouse scrolling. According to the interval, the displacement produced by the canvas is different. View->OnMouseWheel(),OnMouseHWheel()
        private DateTime m_dt_vw = DateTime.Now;
        private DateTime m_dt_hw = DateTime.Now;
        //Current behavior during mouse movement
        private CanvasAction m_ca;
        //Save the selected node
        private HashSet<STNode> m_hs_node_selected = new HashSet<STNode>();

        private bool m_is_process_mouse_event = true;               //Whether to pass mouse-related events downwards (Node or NodeControls), such as disconnection-related operations should not be passed downwards
        private bool m_is_buildpath;                                //Used to determine whether to re-establish the cache connection path during the redrawing process
        private Pen m_p_line = new Pen(Color.Cyan, 2f);             //Used to draw connected lines
        private Pen m_p_line_hover = new Pen(Color.Cyan, 4f);       //Used to draw the line when the mouse is hovering
        private GraphicsPath m_gp_hover;                            //The current connection path where the mouse is hovering
        private StringFormat m_sf = new StringFormat();             //Text format Used to set the text format when Mark draws
        //Save the node relationship corresponding to each connecting line
        private Dictionary<GraphicsPath, ConnectionInfo> m_dic_gp_info = new Dictionary<GraphicsPath, ConnectionInfo>();
        //Save the position of the Node beyond the visual area
        private List<Point> m_lst_node_out = new List<Point>();
        //The Node type loaded in the current editor is used to load nodes from files or data.
        private Dictionary<string, Type> m_dic_type = new Dictionary<string, Type>();

        private int m_time_alert;
        private int m_alpha_alert;
        private string m_str_alert;
        private Color m_forecolor_alert;
        private Color m_backcolor_alert;
        private DateTime m_dt_alert;
        private Rectangle m_rect_alert;
        private AlertLocation m_al;

        #endregion

        #region event ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Occurs when the active node changes.
        /// </summary>
        [Description("Occurs when the active node changes.")]
        public event EventHandler ActiveChanged;
        /// <summary>
        /// Occurs when the selected node changes.
        /// </summary>
        [Description("Occurs when the selected node changes.")]
        public event EventHandler SelectedChanged;
        /// <summary>
        /// Occurs when the hovering node changes.
        /// </summary>
        [Description("Occurs when the hovering node changes.")]
        public event EventHandler HoverChanged;
        /// <summary>
        /// Occurs when a node is added.
        /// </summary>
        [Description("Occurs when a node is added.")]
        public event STNodeEditorEventHandler NodeAdded;
        /// <summary>
        /// Occurs when a node is added.
        /// </summary>
        [Description("Occurs when a node is removed.")]
        public event STNodeEditorEventHandler NodeRemoved;
        /// <summary>
        /// Occurs when the origin of the canvas is moved.
        /// </summary>
        [Description("Occurs when the origin of the canvas is moved.")]
        public event EventHandler CanvasMoved;
        /// <summary>
        /// Occurs when the canvas is zoomed.
        /// </summary>
        [Description("Occurs when the canvas is zoomed.")]
        public event EventHandler CanvasZoomed;
        /// <summary>
        /// Occurs when connecting node options.
        /// </summary>
        [Description("Occurs when connecting node options.")]
        public event STNodeEditorOptionEventHandler OptionConnected;
        /// <summary>
        /// Occurs when connecting node options.
        /// </summary>
        [Description("Occurs when connecting node options.")]
        public event STNodeEditorOptionEventHandler OptionConnecting;
        /// <summary>
        /// Occurs when the node is disconnected.
        /// </summary>
        [Description("Occurs when the node is disconnected.")]
        public event STNodeEditorOptionEventHandler OptionDisconnected;
        /// <summary>
        /// Occurs when the node option is being disconnected.
        /// </summary>
        [Description("Occurs when the node option is being disconnected.")]
        public event STNodeEditorOptionEventHandler OptionDisconnecting;
        /// <summary>
        /// Occurs when one or more nodes have been moved around the canvas.
        /// </summary>
        [Description("Occurs when one or more nodes have been moved around the canvas.")]
        public event STNodesMovedEventHandler NodesMoved;

        protected virtual internal void OnSelectedChanged(EventArgs e) {
            if (this.SelectedChanged != null) this.SelectedChanged(this, e);
        }
        protected virtual void OnActiveChanged(EventArgs e) {
            if (this.ActiveChanged != null) this.ActiveChanged(this, e);
        }
        protected virtual void OnHoverChanged(EventArgs e) {
            if (this.HoverChanged != null) this.HoverChanged(this, e);
        }
        protected internal virtual void OnNodeAdded(STNodeEditorEventArgs e) {
            if (this.NodeAdded != null) this.NodeAdded(this, e);
        }
        protected internal virtual void OnNodeRemoved(STNodeEditorEventArgs e) {
            if (this.NodeRemoved != null) this.NodeRemoved(this, e);
        }
        protected virtual void OnCanvasMoved(EventArgs e) {
            if (this.CanvasMoved != null) this.CanvasMoved(this, e);
        }
        protected virtual void OnCanvasZoomed(EventArgs e) {
            if (this.CanvasZoomed != null) this.CanvasZoomed(this, e);
        }
        protected internal virtual void OnOptionConnected(STNodeEditorOptionEventArgs e) {
            if (this.OptionConnected != null) this.OptionConnected(this, e);
        }
        protected internal virtual void OnOptionDisconnected(STNodeEditorOptionEventArgs e) {
            if (this.OptionDisconnected != null) this.OptionDisconnected(this, e);
        }
        protected internal virtual void OnOptionConnecting(STNodeEditorOptionEventArgs e) {
            if (this.OptionConnecting != null) this.OptionConnecting(this, e);
        }
        protected internal virtual void OnOptionDisconnecting(STNodeEditorOptionEventArgs e) {
            if (this.OptionDisconnecting != null) this.OptionDisconnecting(this, e);
        }
        protected internal virtual void OnNodesMoved(STNodesMovedEventArgs e) {
            if (this.NodesMoved != null) this.NodesMoved(this, e);
        }

        #endregion event

        #region override -----------------------------------------------------------------------------------------------------

        protected override void OnCreateControl() {
            m_drawing_tools = new DrawingTools() {
                Pen = new Pen(Color.Black, 1),
                SolidBrush = new SolidBrush(Color.Black)
            };
            m_img_border = this.CreateBorderImage(this._BorderColor);
            m_img_border_active = this.CreateBorderImage(this._BorderActiveColor);
            m_img_border_hover = this.CreateBorderImage(this._BorderHoverColor);
            m_img_border_selected = this.CreateBorderImage(this._BorderSelectedColor);

            m_pen_border = new Pen(new SolidBrush(Color.FromArgb(50, this._BorderColor)), 4.0f);
            m_pen_border_active = new Pen(new SolidBrush(Color.FromArgb(150, this._BorderActiveColor)), 2.0f);
            m_pen_border_hover = new Pen(new SolidBrush(Color.FromArgb(150, this._BorderHoverColor)), 2.0f);
            m_pen_border_selected = new Pen(new SolidBrush(Color.FromArgb(150, this._BorderSelectedColor)), 2.0f);

            base.OnCreateControl();
            new Thread(this.MoveCanvasThread) { IsBackground = true }.Start();
            new Thread(this.ShowAlertThread) { IsBackground = true }.Start();
            m_sf = new StringFormat();
            m_sf.Alignment = StringAlignment.Near;
            m_sf.FormatFlags = StringFormatFlags.NoWrap;
            m_sf.SetTabStops(0, new float[] { 40 });
        }

        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);
            try {
                if (m.Msg == WM_MOUSEHWHEEL) { //Get horizontal scrolling message

                    Point pt = new Point(((int)m.LParam) >> 16, (ushort)m.LParam);
                    pt = this.PointToClient(pt);

                    MouseButtons mb = MouseButtons.None;
                    int n = (ushort)m.WParam;
                    if ((n & 0x0001) == 0x0001) mb |= MouseButtons.Left;
                    if ((n & 0x0010) == 0x0010) mb |= MouseButtons.Middle;
                    if ((n & 0x0002) == 0x0002) mb |= MouseButtons.Right;
                    if ((n & 0x0020) == 0x0020) mb |= MouseButtons.XButton1;
                    if ((n & 0x0040) == 0x0040) mb |= MouseButtons.XButton2;
                    this.OnMouseHWheel(new MouseEventArgs(mb, 0, pt.X, pt.Y, ((int)m.WParam) >> 16));
                }
            } catch { /*add code*/ }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(this.BackColor);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            m_drawing_tools.Graphics = g;
            SolidBrush brush = m_drawing_tools.SolidBrush;

            if (this._ShowGrid) this.OnDrawGrid(m_drawing_tools, this.Width, this.Height);

            g.TranslateTransform(this._CanvasOffsetX, this._CanvasOffsetY); //Move the coordinate system
            g.ScaleTransform(this._CanvasScale, this._CanvasScale);         //Scale the drawing surface

            this.OnDrawConnectedLine(m_drawing_tools);
            this.OnDrawNode(m_drawing_tools, this.ControlToCanvas(this.ClientRectangle));

            if (m_ca == CanvasAction.ConnectOption) {                       //If you are connecting
                m_drawing_tools.Pen.Color = this._HighLineColor;
                g.SmoothingMode = SmoothingMode.HighQuality;
                if (m_option_down.IsInput)
                    this.DrawBezier(g, m_drawing_tools.Pen, m_pt_in_canvas, m_pt_dot_down, this._Curvature);
                else
                    this.DrawBezier(g, m_drawing_tools.Pen, m_pt_dot_down, m_pt_in_canvas, this._Curvature);
            }
            //Reset the drawing coordinates. I think that other than the nodes, the decoration related drawing should not be drawn in the Canvas coordinate system, but the coordinates of the control should be used for drawing, otherwise it will be affected by the zoom ratio.
            g.ResetTransform();

            switch (m_ca) {
                case CanvasAction.MoveNode:                                 //Draw alignment guides during movement
                    if (this._ShowMagnet && this._ActiveNode != null) this.OnDrawMagnet(m_drawing_tools, m_mi);
                    break;
                case CanvasAction.SelectRectangle:                          //Draw rectangle selection
                    this.OnDrawSelectedRectangle(m_drawing_tools, this.CanvasToControl(m_rect_select));
                    break;
                case CanvasAction.DrawMarkDetails:                          //Draw marker information details
                    if (!string.IsNullOrEmpty(m_find.Mark)) this.OnDrawMark(m_drawing_tools);
                    break;
            }

            if (this._ShowLocation) this.OnDrawNodeOutLocation(m_drawing_tools, this.Size, m_lst_node_out);
            this.OnDrawAlert(g);
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            this.Focus();
            m_ca = CanvasAction.None;
            m_mi.XMatched = m_mi.YMatched = false;
            m_pt_down_in_control = e.Location;
            m_pt_down_in_canvas.X = ((e.X - this._CanvasOffsetX) / this._CanvasScale);
            m_pt_down_in_canvas.Y = ((e.Y - this._CanvasOffsetY) / this._CanvasScale);
            m_pt_canvas_old.X = this._CanvasOffsetX;
            m_pt_canvas_old.Y = this._CanvasOffsetY;

            if (m_gp_hover != null && e.Button == MouseButtons.Right) {     //Disconnect
                this.DisconnectionHover();
                m_is_process_mouse_event = false; //Terminate MouseClick and MouseUp to pass down
                return;
            }

            NodeFindInfo nfi = this.FindNodeFromPoint(m_pt_down_in_canvas);
            if (!string.IsNullOrEmpty(nfi.Mark)) {                          //If the click is marked information
                m_ca = CanvasAction.DrawMarkDetails;
                this.Invalidate();
                return;
            }

            if (nfi.NodeOption != null) {                                   //If you click the option connection point
                this.StartConnect(nfi.NodeOption);
                return;
            }

            if (nfi.Node != null) {
                nfi.Node.OnMouseDown(new MouseEventArgs(e.Button, e.Clicks, (int)m_pt_down_in_canvas.X - nfi.Node.Left, (int)m_pt_down_in_canvas.Y - nfi.Node.Top, e.Delta));

                if (e.Button == MouseButtons.Left)
                {
                    bool bCtrlDown = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                    if (bCtrlDown)
                    {
                        if (nfi.Node.IsSelected)
                        {
                            if (nfi.Node == this._ActiveNode)
                            {
                                this.SetActiveNode(null);
                            }
                        }
                        else
                        {
                            nfi.Node.SetSelected(true, true);
                        }
                        return;
                    }
                    else if (!nfi.Node.IsSelected)
                    {
                        foreach (var n in m_hs_node_selected.ToArray()) n.SetSelected(false, false);
                    }
                    nfi.Node.SetSelected(true, false);                      //Add to selected node
                    this.SetActiveNode(nfi.Node);
                    if (this.PointInRectangle(nfi.Node.TitleRectangle, m_pt_down_in_canvas.X, m_pt_down_in_canvas.Y))
                    {
                        if (e.Button == MouseButtons.Right)
                        {
                            if (nfi.Node.ContextMenuStrip != null)
                            {
                                nfi.Node.ContextMenuStrip.Show(this.PointToScreen(e.Location));
                            }
                        }
                        else
                        {
                            m_dic_pt_selected.Clear();
                            lock (m_hs_node_selected)
                            {
                                foreach (STNode n in m_hs_node_selected)    //Record the position of the selected node. It will be useful if you need to move the selected node.
                                    m_dic_pt_selected.Add(n, n.Location);
                            }
                            m_ca = CanvasAction.MoveNode;                   //If you click the title of the node, you can move the node
                            if (this._ShowMagnet && this._ActiveNode != null) this.BuildMagnetLocation();   //It will be useful to establish the coordinates required for the magnet if you need to move the selected node
                        }
                    }
                    else
                        m_node_down = nfi.Node;
                }
            } else {
                if (e.Button == MouseButtons.Left)
                {
                    this.SetActiveNode(null);
                    foreach (var n in m_hs_node_selected.ToArray()) n.SetSelected(false, false);//Did not click anything to clear the selected node
                    m_ca = CanvasAction.SelectRectangle;                    //Enter rectangular area selection mode
                    m_rect_select.Width = m_rect_select.Height = 0;
                    m_node_down = null;
                }
            }
            //this.SetActiveNode(nfi.Node);
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            m_pt_in_control = e.Location;
            m_pt_in_canvas.X = ((e.X - this._CanvasOffsetX) / this._CanvasScale);
            m_pt_in_canvas.Y = ((e.Y - this._CanvasOffsetY) / this._CanvasScale);

            if (e.Button == MouseButtons.Middle)
            {  //Move the canvas with the middle mouse button
                this._CanvasOffsetX = m_real_canvas_x = m_pt_canvas_old.X + (e.X - m_pt_down_in_control.X);
                this._CanvasOffsetY = m_real_canvas_y = m_pt_canvas_old.Y + (e.Y - m_pt_down_in_control.Y);
                this.Invalidate();
                return;
            }

            if (m_node_down != null) {
                m_node_down.OnMouseMove(new MouseEventArgs(e.Button, e.Clicks,
                    (int)m_pt_in_canvas.X - m_node_down.Left,
                    (int)m_pt_in_canvas.Y - m_node_down.Top, e.Delta));
                return;
            }

            
            if (e.Button == MouseButtons.Left) {    //If the left mouse button is clicked, the behavior is judged
                m_gp_hover = null;
                switch (m_ca) {
                    case CanvasAction.MoveNode: this.MoveNode(e.Location); return;  //Current mobile node
                    case CanvasAction.ConnectOption: this.Invalidate(); return;     //Currently connecting
                    case CanvasAction.SelectRectangle:                              //Currently selected
                        m_rect_select.X = m_pt_down_in_canvas.X < m_pt_in_canvas.X ? m_pt_down_in_canvas.X : m_pt_in_canvas.X;
                        m_rect_select.Y = m_pt_down_in_canvas.Y < m_pt_in_canvas.Y ? m_pt_down_in_canvas.Y : m_pt_in_canvas.Y;
                        m_rect_select.Width = Math.Abs(m_pt_in_canvas.X - m_pt_down_in_canvas.X);
                        m_rect_select.Height = Math.Abs(m_pt_in_canvas.Y - m_pt_down_in_canvas.Y);
                        foreach (STNode n in this._Nodes) {
                            n.SetSelected(m_rect_select.IntersectsWith(n.Rectangle), false);
                        }
                        this.Invalidate();
                        return;
                }
            }
            //If there is no behavior, judge whether there are other objects under the mouse
            NodeFindInfo nfi = this.FindNodeFromPoint(m_pt_in_canvas);
            bool bRedraw = false;
            if (this._HoverNode != nfi.Node) {          //Hover over Node
                if (nfi.Node != null) nfi.Node.OnMouseEnter(EventArgs.Empty);
                if (this._HoverNode != null)
                    this._HoverNode.OnMouseLeave(new MouseEventArgs(e.Button, e.Clicks,
                        (int)m_pt_in_canvas.X - this._HoverNode.Left,
                        (int)m_pt_in_canvas.Y - this._HoverNode.Top, e.Delta));
                this._HoverNode = nfi.Node;
                this.OnHoverChanged(EventArgs.Empty);
                bRedraw = true;
            }
            if (this._HoverNode != null) {
                this._HoverNode.OnMouseMove(new MouseEventArgs(e.Button, e.Clicks,
                    (int)m_pt_in_canvas.X - this._HoverNode.Left,
                    (int)m_pt_in_canvas.Y - this._HoverNode.Top, e.Delta));
                m_gp_hover = null;
            } else {
                GraphicsPath gp = null;
                foreach (var v in m_dic_gp_info) {          //Determine whether the mouse is hovering over the connection path
                    if (v.Key.IsOutlineVisible(m_pt_in_canvas, m_p_line_hover)) {
                        gp = v.Key;
                        break;
                    }
                }
                if (m_gp_hover != gp) {
                    m_gp_hover = gp;
                    bRedraw = true;
                }
            }
            if (bRedraw) this.Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            var nfi = this.FindNodeFromPoint(m_pt_in_canvas);
            switch (m_ca) {                         //Judging behavior when the mouse is raised
                case CanvasAction.MoveNode:         //If you are moving Node, send NodesMoved event and re-record the current position
                    {
                        List<NodeMovement> movements = new List<NodeMovement>();
                        bool didNodesReallyMove = false;

                        foreach (STNode n in m_dic_pt_selected.Keys.ToList())
                        {
                            NodeMovement movement = new NodeMovement();
                            movement.Node = n;
                            movement.OldLocation = m_dic_pt_selected[n];
                            movement.NewLocation = n.Location;
                            movements.Add(movement);

                            if (movement.OldLocation != movement.NewLocation)
                                didNodesReallyMove = true;
                        }

                        if (didNodesReallyMove)
                            OnNodesMoved(new STNodesMovedEventArgs(movements.ToArray()));

                        foreach (STNode n in m_dic_pt_selected.Keys.ToList())
                            m_dic_pt_selected[n] = n.Location;
                    }
                    break;
                case CanvasAction.ConnectOption:    //If it is connecting, end the connection
                    if (e.Location == m_pt_down_in_control) break;
                    if (nfi.NodeOption != null) {
                        if (m_option_down.IsInput)
                            nfi.NodeOption.ConnectOption(m_option_down);
                        else
                            m_option_down.ConnectOption(nfi.NodeOption);
                    }
                    break;
            }
            if (m_is_process_mouse_event && this._ActiveNode != null) {
                var mea = new MouseEventArgs(e.Button, e.Clicks,
                    (int)m_pt_in_canvas.X - this._ActiveNode.Left,
                    (int)m_pt_in_canvas.Y - this._ActiveNode.Top, e.Delta);
                this._ActiveNode.OnMouseUp(mea);
                m_node_down = null;
            }
            m_is_process_mouse_event = true;        //The current disconnection operation does not carry out event delivery, and the event will be accepted next time
            m_ca = CanvasAction.None;
            this.Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            m_mouse_in_control = true;
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            m_mouse_in_control = false;
            if (this._HoverNode != null) this._HoverNode.OnMouseLeave(e);
            this._HoverNode = null;
            this.Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);

            bool zoomInput = !mRequireCtrlForZooming || ((Control.ModifierKeys & Keys.Control) == Keys.Control);

            if (zoomInput) {
                float f = this._CanvasScale + (e.Delta < 0 ? -0.1f : 0.1f);
                this.ScaleCanvas(f, this.Width / 2, this.Height / 2);
            } /*else {
                if (!m_mouse_in_control) return;
                var nfi = this.FindNodeFromPoint(m_pt_in_canvas);
                if (this._HoverNode != null) {
                    this._HoverNode.OnMouseWheel(new MouseEventArgs(e.Button, e.Clicks,
                        (int)m_pt_in_canvas.X - this._HoverNode.Left,
                        (int)m_pt_in_canvas.Y - this._HoverNode.Top, e.Delta));
                    return;
                }
                int t = (int)DateTime.Now.Subtract(m_dt_vw).TotalMilliseconds;
                if (t <= 30) t = 40;
                else if (t <= 100) t = 20;
                else if (t <= 150) t = 10;
                else if (t <= 300) t = 4;
                else t = 2;
                this.MoveCanvas(this._CanvasOffsetX, m_real_canvas_y + (e.Delta < 0 ? -t : t), true, CanvasMoveArgs.Top);//process mouse mid
                m_dt_vw = DateTime.Now;
            }*/
        }

        protected virtual void OnMouseHWheel(MouseEventArgs e) {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control) return;
            if (!m_mouse_in_control) return;
            if (this._HoverNode != null) {
                this._HoverNode.OnMouseWheel(new MouseEventArgs(e.Button, e.Clicks,
                    (int)m_pt_in_canvas.X - this._HoverNode.Left,
                    (int)m_pt_in_canvas.Y - this._HoverNode.Top, e.Delta));
                return;
            }
            int t = (int)DateTime.Now.Subtract(m_dt_hw).TotalMilliseconds;
            if (t <= 30) t = 40;
            else if (t <= 100) t = 20;
            else if (t <= 150) t = 10;
            else if (t <= 300) t = 4;
            else t = 2;
            this.MoveCanvas(m_real_canvas_x + (e.Delta > 0 ? -t : t), this._CanvasOffsetY, true, CanvasMoveArgs.Left);
            m_dt_hw = DateTime.Now;
        }
        //===========================for node other event==================================
        protected override void OnMouseClick(MouseEventArgs e) {
            base.OnMouseClick(e);
            if (this._ActiveNode != null && m_is_process_mouse_event) {
                if (!this.PointInRectangle(this._ActiveNode.Rectangle, m_pt_in_canvas.X, m_pt_in_canvas.Y)) return;

                PointF pointInCanvas = new PointF();
                pointInCanvas.X = ((e.X - this._CanvasOffsetX) / this._CanvasScale);
                pointInCanvas.Y = ((e.Y - this._CanvasOffsetY) / this._CanvasScale);
                NodeFindInfo nfi = this.FindNodeFromPoint(pointInCanvas);

                if (nfi.NodeOption != null)
                {
                    //System.Diagnostics.Debug.Assert(nfi.Node == this._ActiveNode);
                    //System.Diagnostics.Trace.WriteLine("Clearing active ctrl");

                    // If we hit an option, the option takes
                    // precedence so clear the active control.
                    this._ActiveNode.ClearActiveCtrl();
                }

                this._ActiveNode.OnMouseClick(new MouseEventArgs(e.Button, e.Clicks,
                    (int)m_pt_down_in_canvas.X - this._ActiveNode.Left,
                    (int)m_pt_down_in_canvas.Y - this._ActiveNode.Top, e.Delta));
            }
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (this._ActiveNode != null) this._ActiveNode.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            base.OnKeyUp(e);
            if (this._ActiveNode != null) this._ActiveNode.OnKeyUp(e);
            m_node_down = null;
        }

        protected override void OnKeyPress(KeyPressEventArgs e) {
            base.OnKeyPress(e);
            if (this._ActiveNode != null) this._ActiveNode.OnKeyPress(e);
        }

        #endregion

        protected override void OnDragEnter(DragEventArgs drgevent) {
            base.OnDragEnter(drgevent);
            if (this.DesignMode) return;
            if (drgevent.Data.GetDataPresent("STNodeType"))
                drgevent.Effect = DragDropEffects.Copy;
            else
                drgevent.Effect = DragDropEffects.None;

        }

        protected override void OnDragDrop(DragEventArgs drgevent) {
            base.OnDragDrop(drgevent);
            if (this.DesignMode) return;
            if (drgevent.Data.GetDataPresent("STNodeType")) {
                object data = drgevent.Data.GetData("STNodeType");
                if (!(data is Type)) return;
                var t = (Type)data;
                if (!t.IsSubclassOf(typeof(STNode))) return;
                STNode node = (STNode)Activator.CreateInstance((t));
                Point pt = new Point(drgevent.X, drgevent.Y);
                pt = this.PointToClient(pt);
                pt = this.ControlToCanvas(pt);
                node.Left = pt.X; node.Top = pt.Y;
                this.Nodes.Add(node);
            }
        }

        #region protected ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Occurs when the background grid lines are drawn.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        /// <param name="nWidth">Need to draw width</param>
        /// <param name="nHeight">Need to draw height</param>
        protected virtual void OnDrawGrid(DrawingTools dt, int nWidth, int nHeight) {
            Graphics g = dt.Graphics;
            using (Pen p_2 = new Pen(Color.FromArgb(65, this._GridColor))) {
                using (Pen p_1 = new Pen(Color.FromArgb(30, this._GridColor))) {
                    float nIncrement = (20 * this._CanvasScale);             //The interval between the grids is drawn according to the scale
                    int n = 5 - (int)(this._CanvasOffsetX / nIncrement);
                    for (float f = this._CanvasOffsetX % nIncrement; f < nWidth; f += nIncrement)
                        g.DrawLine((n++ % 5 == 0 ? p_2 : p_1), f, 0, f, nHeight);
                    n = 5 - (int)(this._CanvasOffsetY / nIncrement);
                    for (float f = this._CanvasOffsetY % nIncrement; f < nHeight; f += nIncrement)
                        g.DrawLine((n++ % 5 == 0 ? p_2 : p_1), 0, f, nWidth, f);
                    //Two antennas at origin
                    p_1.Color = Color.FromArgb(this._Nodes.Count == 0 ? 255 : 120, this._GridColor);
                    g.DrawLine(p_1, this._CanvasOffsetX, 0, this._CanvasOffsetX, nHeight);
                    g.DrawLine(p_1, 0, this._CanvasOffsetY, nWidth, this._CanvasOffsetY);
                }
            }
        }
        /// <summary>
        /// Occurs when the Node is drawn.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        /// <param name="rect">Viewable canvas area size</param>
        protected virtual void OnDrawNode(DrawingTools dt, Rectangle rect) {
            m_lst_node_out.Clear(); //Clear the coordinates of the Node beyond the visual area
            foreach (STNode n in this._Nodes) {
                if (this._ShowBorder) this.OnDrawNodeBorder(dt, n);
                n.OnDrawNode(dt);                                       //Call Node to draw the main part of itself
                if (!string.IsNullOrEmpty(n.Mark)) n.OnDrawMark(dt);    //Call Node to draw the Mark area by itself
                if (!rect.IntersectsWith(n.Rectangle)) {
                    m_lst_node_out.Add(n.Location);                     //Determine whether this Node exceeds the visual area
                }
            }
        }
        /// <summary>
        /// Occurs when the Node border is drawn.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        /// <param name="node">Target node</param>
        protected virtual void OnDrawNodeBorder(DrawingTools dt, STNode node) {
            if (mRoundedCornerRadius == -1)
            {
                Image img_border = null;
                if (this._ActiveNode == node) img_border = m_img_border_active;
                else if (node.IsSelected) img_border = m_img_border_selected;
                else if (this._HoverNode == node) img_border = m_img_border_hover;
                else img_border = m_img_border;
                this.RenderBorder(dt.Graphics, node.Rectangle, img_border);
                if (!string.IsNullOrEmpty(node.Mark)) this.RenderBorder(dt.Graphics, node.MarkRectangle, img_border);
            }
            else
            {
                Rectangle borderRect = node.Rectangle;

                Pen pen_border = null;
                bool highlight = true;

                if (this._ActiveNode == node) pen_border = m_pen_border_active;
                else if (node.IsSelected) pen_border = m_pen_border_selected;
                else if (this._HoverNode == node) pen_border = m_pen_border_hover;
                else
                {
                    pen_border = m_pen_border;
                    highlight = false;
                }

                if (highlight)
                {
                    borderRect.Inflate(2, 2);
                    RoundedCornerUtils.DrawRoundedRectangle(dt.Graphics, pen_border, borderRect, mRoundedCornerRadius + 2);
                }
                else
                {
                    //borderRect.X += 2;
                    //borderRect.Y += 1;
                    RoundedCornerUtils.DrawRoundedRectangle(dt.Graphics, pen_border, borderRect, mRoundedCornerRadius);
                }
            }
        }
        /// <summary>
        /// Occurs when drawing a connected path.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        protected virtual void OnDrawConnectedLine(DrawingTools dt) {
            Graphics g = dt.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            m_p_line_hover.Color = Color.FromArgb(50, 0, 0, 0);
            var t = typeof(object);
            foreach (STNode n in this._Nodes) {
                foreach (STNodeOption op in n.OutputOptions) {
                    if (op == STNodeOption.Empty) continue;
                    if (op.DotColor != Color.Transparent)       //Determine the line color
                        m_p_line.Color = op.DotColor;
                    else {
                        if (op.DataType == t)
                            m_p_line.Color = this._UnknownTypeColor;
                        else
                            m_p_line.Color = this._TypeColor.ContainsKey(op.DataType) ? this._TypeColor[op.DataType] : this._UnknownTypeColor;//value can not be null
                    }
                    foreach (var v in op.ConnectedOption) {
                        this.DrawBezier(g, m_p_line_hover, op.DotLeft + op.DotSize, op.DotTop + op.DotSize / 2,
                            v.DotLeft - 1, v.DotTop + v.DotSize / 2, this._Curvature);
                        this.DrawBezier(g, m_p_line, op.DotLeft + op.DotSize, op.DotTop + op.DotSize / 2,
                            v.DotLeft - 1, v.DotTop + v.DotSize / 2, this._Curvature);
                        if (m_is_buildpath) {                       //If the current drawing needs to re-establish the connected path cache
                            GraphicsPath gp = this.CreateBezierPath(op.DotLeft + op.DotSize, op.DotTop + op.DotSize / 2,
                                v.DotLeft - 1, v.DotTop + v.DotSize / 2, this._Curvature);
                            m_dic_gp_info.Add(gp, new ConnectionInfo() { Output = op, Input = v });
                        }
                    }
                }
            }
            m_p_line_hover.Color = this._HighLineColor;
            if (m_gp_hover != null)
            {       //If there is currently a hovering link, highlight it and draw it

                // The call to DrawPath() occasionally crashes. It would seem like m_gp_hover has somehow been disposed of or something
                // but I haven't been able to reproduce it reliably, so I'll slap a try-catch around the call for now...
                try
                {
                    g.DrawPath(m_p_line_hover, m_gp_hover);
                }
                catch (Exception /*ex*/) { }
            }
            m_is_buildpath = false;         //Reset the flag, the path cache will not be re-established the next time you draw
        }
        /// <summary>
        /// Occurs when drawing the Mark details.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        protected virtual void OnDrawMark(DrawingTools dt) {
            Graphics g = dt.Graphics;
            SizeF sz = g.MeasureString(m_find.Mark, this.Font);             //Confirm the required size of the text
            Rectangle rect = new Rectangle(m_pt_in_control.X + 15,
                m_pt_in_control.Y + 10,
                (int)sz.Width + 6,
                4 + (this.Font.Height + 4) * m_find.MarkLines.Length);      //sz.Height does not consider the line spacing of the text, so here the height is calculated by itself

            if (rect.Right > this.Width) rect.X = this.Width - rect.Width;
            if (rect.Bottom > this.Height) rect.Y = this.Height - rect.Height;
            if (rect.X < 0) rect.X = 0;
            if (rect.Y < 0) rect.Y = 0;

            dt.SolidBrush.Color = this._MarkBackColor;
            g.SmoothingMode = SmoothingMode.None;
            g.FillRectangle(dt.SolidBrush, rect);                             //Draw background area
            rect.Width--; rect.Height--;
            dt.Pen.Color = Color.FromArgb(255, this._MarkBackColor);
            g.DrawRectangle(dt.Pen, rect);
            dt.SolidBrush.Color = this._MarkForeColor;

            m_sf.LineAlignment = StringAlignment.Center;
            //g.SmoothingMode = SmoothingMode.HighQuality;
            rect.X += 2; rect.Width -= 3;
            rect.Height = this.Font.Height + 4;
            int nY = rect.Y + 2;
            for (int i = 0; i < m_find.MarkLines.Length; i++) {             //Draw text
                rect.Y = nY + i * (this.Font.Height + 4);
                g.DrawString(m_find.MarkLines[i], this.Font, dt.SolidBrush, rect, m_sf);
            }
        }
        /// <summary>
        /// Occurs when the alignment guide line needs to be displayed when moving the Node.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        /// <param name="mi">Matching magnet information</param>
        protected virtual void OnDrawMagnet(DrawingTools dt, MagnetInfo mi) {
            if (this._ActiveNode == null) return;
            Graphics g = dt.Graphics;
            Pen pen = m_drawing_tools.Pen;
            SolidBrush brush = dt.SolidBrush;
            pen.Color = this._MagnetColor;
            brush.Color = Color.FromArgb(this._MagnetColor.A / 3, this._MagnetColor);
            g.SmoothingMode = SmoothingMode.None;
            int nL = this._ActiveNode.Left, nMX = this._ActiveNode.Left + this._ActiveNode.Width / 2, nR = this._ActiveNode.Right;
            int nT = this._ActiveNode.Top, nMY = this._ActiveNode.Top + this._ActiveNode.Height / 2, nB = this._ActiveNode.Bottom;
            if (mi.XMatched) g.DrawLine(pen, this.CanvasToControl(mi.X, true), 0, this.CanvasToControl(mi.X, true), this.Height);
            if (mi.YMatched) g.DrawLine(pen, 0, this.CanvasToControl(mi.Y, false), this.Width, this.CanvasToControl(mi.Y, false));
            g.TranslateTransform(this._CanvasOffsetX, this._CanvasOffsetY); //Move the coordinate system
            g.ScaleTransform(this._CanvasScale, this._CanvasScale);         //Scale the drawing surface
            if (mi.XMatched) {
                //g.DrawLine(pen, this.CanvasToControl(mi.X, true), 0, this.CanvasToControl(mi.X, true), this.Height);
                foreach (STNode n in this._Nodes) {
                    if (n.Left == mi.X || n.Right == mi.X || n.Left + n.Width / 2 == mi.X) {
                        //g.DrawRectangle(pen, n.Left, n.Top, n.Width - 1, n.Height - 1);
                        if (mRoundedCornerRadius == -1)
                        {
                            g.FillRectangle(brush, n.Rectangle);
                        }
                        else
                        {
                            RoundedCornerUtils.FillRoundedRectangle(g, brush, n.Rectangle, mRoundedCornerRadius);
                        }
                    }
                }
            }
            if (mi.YMatched) {
                //g.DrawLine(pen, 0, this.CanvasToControl(mi.Y, false), this.Width, this.CanvasToControl(mi.Y, false));
                foreach (STNode n in this._Nodes) {
                    if (n.Top == mi.Y || n.Bottom == mi.Y || n.Top + n.Height / 2 == mi.Y) {
                        //g.DrawRectangle(pen, n.Left, n.Top, n.Width - 1, n.Height - 1);
                        if (mRoundedCornerRadius == -1)
                        {
                            g.FillRectangle(brush, n.Rectangle);
                        }
                        else
                        {
                            RoundedCornerUtils.FillRoundedRectangle(g, brush, n.Rectangle, mRoundedCornerRadius);
                        }
                    }
                }
            }
            g.ResetTransform();
        }
        /// <summary>
        /// Draw selected rectangular area.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        /// <param name="rectf">Rectangular area located on the control</param>
        protected virtual void OnDrawSelectedRectangle(DrawingTools dt, RectangleF rectf) {
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;
            dt.Pen.Color = this._SelectedRectangleColor;
            g.DrawRectangle(dt.Pen, rectf.Left, rectf.Y, rectf.Width, rectf.Height);
            brush.Color = Color.FromArgb(this._SelectedRectangleColor.A / 3, this._SelectedRectangleColor);
            g.FillRectangle(brush, this.CanvasToControl(m_rect_select));
        }
        /// <summary>
        /// Draw the Node position prompt message beyond the visual area.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        /// <param name="sz">Tip box margin</param>
        /// <param name="lstPts">Node location information beyond the visual area</param>
        protected virtual void OnDrawNodeOutLocation(DrawingTools dt, Size sz, List<Point> lstPts) {
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;
            brush.Color = this._LocationBackColor;
            g.SmoothingMode = SmoothingMode.None;
            if (lstPts.Count == this._Nodes.Count && this._Nodes.Count != 0) {  //If the number of excesses is as much as the number of sets, all are exceeded. Draw an outer rectangle
                g.FillRectangle(brush, this.CanvasToControl(this._CanvasValidBounds));
            }
            g.FillRectangle(brush, 0, 0, 4, sz.Height);                       //Draw a four-sided background
            g.FillRectangle(brush, sz.Width - 4, 0, 4, sz.Height);
            g.FillRectangle(brush, 4, 0, sz.Width - 8, 4);
            g.FillRectangle(brush, 4, sz.Height - 4, sz.Width - 8, 4);
            brush.Color = this._LocationForeColor;
            foreach (var v in lstPts) {                                         //Draw points
                var pt = this.CanvasToControl(v);
                if (pt.X < 0) pt.X = 0;
                if (pt.Y < 0) pt.Y = 0;
                if (pt.X > sz.Width) pt.X = sz.Width - 4;
                if (pt.Y > sz.Height) pt.Y = sz.Height - 4;
                g.FillRectangle(brush, pt.X, pt.Y, 4, 4);
            }
        }
        /// <summary>
        /// Drawing prompt message.
        /// </summary>
        /// <param name="dt">Drawing tools</param>
        /// <param name="rect">Need to draw area</param>
        /// <param name="strText">Need to draw text</param>
        /// <param name="foreColor">Information foreground</param>
        /// <param name="backColor">Information background color</param>
        /// <param name="al">Information location</param>
        protected virtual void OnDrawAlert(DrawingTools dt, Rectangle rect, string strText, Color foreColor, Color backColor, AlertLocation al) {
            if (m_alpha_alert == 0) return;
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;

            g.SmoothingMode = SmoothingMode.None;
            brush.Color = backColor;
            dt.Pen.Color = brush.Color;
            g.FillRectangle(brush, rect);
            g.DrawRectangle(dt.Pen, rect.Left, rect.Top, rect.Width - 1, rect.Height - 1);

            brush.Color = foreColor;
            m_sf.Alignment = StringAlignment.Center;
            m_sf.LineAlignment = StringAlignment.Center;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawString(strText, this.Font, brush, rect, m_sf);
        }
        /// <summary>
        /// Rectangular area that needs to be drawn to get the prompt information.
        /// </summary>
        /// <param name="g">Drawing surface</param>
        /// <param name="strText">Need to draw text</param>
        /// <param name="al">Information location</param>
        /// <returns>Rectangular area</returns>
        protected virtual Rectangle GetAlertRectangle(Graphics g, string strText, AlertLocation al) {
            SizeF szf = g.MeasureString(m_str_alert, this.Font);
            Size sz = new Size((int)Math.Round(szf.Width + 10), (int)Math.Round(szf.Height + 4));
            Rectangle rect = new Rectangle(4, this.Height - sz.Height - 4, sz.Width, sz.Height);

            switch (al) {
                case AlertLocation.Left:
                    rect.Y = (this.Height - sz.Height) >> 1;
                    break;
                case AlertLocation.Top:
                    rect.Y = 4;
                    rect.X = (this.Width - sz.Width) >> 1;
                    break;
                case AlertLocation.Right:
                    rect.X = this.Width - sz.Width - 4;
                    rect.Y = (this.Height - sz.Height) >> 1;
                    break;
                case AlertLocation.Bottom:
                    rect.X = (this.Width - sz.Width) >> 1;
                    break;
                case AlertLocation.Center:
                    rect.X = (this.Width - sz.Width) >> 1;
                    rect.Y = (this.Height - sz.Height) >> 1;
                    break;
                case AlertLocation.LeftTop:
                    rect.X = rect.Y = 4;
                    break;
                case AlertLocation.RightTop:
                    rect.Y = 4;
                    rect.X = this.Width - sz.Width - 4;
                    break;
                case AlertLocation.RightBottom:
                    rect.X = this.Width - sz.Width - 4;
                    break;
            }
            return rect;
        }

        #endregion protected

        #region internal

        internal void BuildLinePath() {
            foreach (var v in m_dic_gp_info) v.Key.Dispose();
            m_dic_gp_info.Clear();
            m_is_buildpath = true;
            this.Invalidate();
        }

        internal void OnDrawAlert(Graphics g) {
            m_rect_alert = this.GetAlertRectangle(g, m_str_alert, m_al);
            Color clr_fore = Color.FromArgb((int)((float)m_alpha_alert / 255 * m_forecolor_alert.A), m_forecolor_alert);
            Color clr_back = Color.FromArgb((int)((float)m_alpha_alert / 255 * m_backcolor_alert.A), m_backcolor_alert);
            this.OnDrawAlert(m_drawing_tools, m_rect_alert, m_str_alert, clr_fore, clr_back, m_al);
        }

        internal void InternalAddSelectedNode(STNode node) {
            node.IsSelected = true;
            lock (m_hs_node_selected) m_hs_node_selected.Add(node);
        }

        internal void InternalRemoveSelectedNode(STNode node) {
            node.IsSelected = false;
            lock (m_hs_node_selected) m_hs_node_selected.Remove(node);
        }

        #endregion internal

        #region private -----------------------------------------------------------------------------------------------------

        private void MoveCanvasThread() {
            bool bRedraw;
            while (true) {
                bRedraw = false;
                if (m_real_canvas_x != this._CanvasOffsetX) {
                    float nx = m_real_canvas_x - this._CanvasOffsetX;
                    float n = Math.Abs(nx) / 10;
                    float nTemp = Math.Abs(nx);
                    if (nTemp <= 4) n = 1;
                    else if (nTemp <= 12) n = 2;
                    else if (nTemp <= 30) n = 3;
                    if (nTemp < 1) this._CanvasOffsetX = m_real_canvas_x;
                    else
                        this._CanvasOffsetX += nx > 0 ? n : -n;
                    bRedraw = true;
                }
                if (m_real_canvas_y != this._CanvasOffsetY) {
                    float ny = m_real_canvas_y - this._CanvasOffsetY;
                    float n = Math.Abs(ny) / 10;
                    float nTemp = Math.Abs(ny);
                    if (nTemp <= 4) n = 1;
                    else if (nTemp <= 12) n = 2;
                    else if (nTemp <= 30) n = 3;
                    if (nTemp < 1)
                        this._CanvasOffsetY = m_real_canvas_y;
                    else
                        this._CanvasOffsetY += ny > 0 ? n : -n;
                    bRedraw = true;
                }
                if (bRedraw) {
                    m_pt_canvas_old.X = this._CanvasOffsetX;
                    m_pt_canvas_old.Y = this._CanvasOffsetY;
                    this.Invalidate();
                    Thread.Sleep(30);
                } else {
                    Thread.Sleep(100);
                }
            }
        }

        private void ShowAlertThread() {
            while (true) {
                int nTime = m_time_alert - (int)DateTime.Now.Subtract(m_dt_alert).TotalMilliseconds;
                if (nTime > 0) {
                    Thread.Sleep(nTime);
                    continue;
                }
                if (nTime < -1000) {
                    if (m_alpha_alert != 0) {
                        m_alpha_alert = 0;
                        this.Invalidate();
                    }
                    Thread.Sleep(100);
                } else {
                    m_alpha_alert = (int)(255 - (-nTime / 1000F) * 255);
                    this.Invalidate(m_rect_alert);
                    Thread.Sleep(50);
                }
            }
        }

        private Image CreateBorderImage(Color clr) {
            Image img = new Bitmap(12, 12);
            using (Graphics g = Graphics.FromImage(img)) {
                g.SmoothingMode = SmoothingMode.HighQuality;
                using (GraphicsPath gp = new GraphicsPath()) {
                    gp.AddEllipse(new Rectangle(0, 0, 11, 11));
                    using (PathGradientBrush b = new PathGradientBrush(gp)) {
                        b.CenterColor = Color.FromArgb(200, clr);
                        b.SurroundColors = new Color[] { Color.FromArgb(10, clr) };
                        g.FillPath(b, gp);
                    }
                }
            }
            return img;
        }

        private ConnectionStatus DisconnectionHover() {
            if (!m_dic_gp_info.ContainsKey(m_gp_hover)) return ConnectionStatus.Disconnected;
            ConnectionInfo ci = m_dic_gp_info[m_gp_hover];
            var ret = ci.Output.DisconnectOption(ci.Input);
            //this.OnOptionDisconnected(new STNodeOptionEventArgs(ci.Output, ci.Input, ret));
            if (ret == ConnectionStatus.Disconnected) {
                m_dic_gp_info.Remove(m_gp_hover);
                m_gp_hover.Dispose();
                m_gp_hover = null;
                this.Invalidate();
            }
            return ret;
        }

        private void StartConnect(STNodeOption op) {
            if (op.IsInput) {
                m_pt_dot_down.X = op.DotLeft;
                m_pt_dot_down.Y = op.DotTop + 5;
            } else {
                m_pt_dot_down.X = op.DotLeft + op.DotSize;
                m_pt_dot_down.Y = op.DotTop + 5;
            }
            m_ca = CanvasAction.ConnectOption;
            m_option_down = op;
        }

        private void MoveNode(Point pt) {
            int nX = (int)((pt.X - m_pt_down_in_control.X) / this._CanvasScale);
            int nY = (int)((pt.Y - m_pt_down_in_control.Y) / this._CanvasScale);
            lock (m_hs_node_selected) {
                foreach (STNode v in m_hs_node_selected) {
                    v.Left = m_dic_pt_selected[v].X + nX;
                    v.Top = m_dic_pt_selected[v].Y + nY;
                }
                if (this._ShowMagnet) {
                    MagnetInfo mi = this.CheckMagnet(this._ActiveNode);
                    if (mi.XMatched) {
                        foreach (STNode v in m_hs_node_selected) v.Left -= mi.OffsetX;
                    }
                    if (mi.YMatched) {
                        foreach (STNode v in m_hs_node_selected) v.Top -= mi.OffsetY;
                    }
                }
            }
            this.Invalidate();
        }

        protected internal virtual void BuildBounds() {
            if (this._Nodes.Count == 0) {
                this._CanvasValidBounds = this.ControlToCanvas(this.DisplayRectangle);
                return;
            }
            int x = int.MaxValue;
            int y = int.MaxValue;
            int r = int.MinValue;
            int b = int.MinValue;
            foreach (STNode n in this._Nodes) {
                if (x > n.Left) x = n.Left;
                if (y > n.Top) y = n.Top;
                if (r < n.Right) r = n.Right;
                if (b < n.Bottom) b = n.Bottom;
            }
            this._CanvasValidBounds.X = x - 60;
            this._CanvasValidBounds.Y = y - 60;
            this._CanvasValidBounds.Width = r - x + 120;
            this._CanvasValidBounds.Height = b - y + 120;
        }

        private bool PointInRectangle(Rectangle rect, float x, float y) {
            if (x < rect.Left) return false;
            if (x > rect.Right) return false;
            if (y < rect.Top) return false;
            if (y > rect.Bottom) return false;
            return true;
        }

        private void BuildMagnetLocation() {
            m_lst_magnet_x.Clear();
            m_lst_magnet_y.Clear();
            foreach (STNode v in this._Nodes) {
                if (v.IsSelected) continue;
                m_lst_magnet_x.Add(v.Left);
                m_lst_magnet_x.Add(v.Left + v.Width / 2);
                m_lst_magnet_x.Add(v.Left + v.Width);
                m_lst_magnet_y.Add(v.Top);
                m_lst_magnet_y.Add(v.Top + v.Height / 2);
                m_lst_magnet_y.Add(v.Top + v.Height);
            }
        }

        private MagnetInfo CheckMagnet(STNode node) {
            m_mi.XMatched = m_mi.YMatched = false;
            m_lst_magnet_mx.Clear();
            m_lst_magnet_my.Clear();
            m_lst_magnet_mx.Add(node.Left + node.Width / 2);
            m_lst_magnet_mx.Add(node.Left);
            m_lst_magnet_mx.Add(node.Left + node.Width);
            m_lst_magnet_my.Add(node.Top + node.Height / 2);
            m_lst_magnet_my.Add(node.Top);
            m_lst_magnet_my.Add(node.Top + node.Height);

            bool bFlag = false;
            foreach (var mx in m_lst_magnet_mx) {
                foreach (var x in m_lst_magnet_x) {
                    if (Math.Abs(mx - x) <= 5) {
                        bFlag = true;
                        m_mi.X = x;
                        m_mi.OffsetX = mx - x;
                        m_mi.XMatched = true;
                        break;
                    }
                }
                if (bFlag) break;
            }
            bFlag = false;
            foreach (var my in m_lst_magnet_my) {
                foreach (var y in m_lst_magnet_y) {
                    if (Math.Abs(my - y) <= 5) {
                        bFlag = true;
                        m_mi.Y = y;
                        m_mi.OffsetY = my - y;
                        m_mi.YMatched = true;
                        break;
                    }
                }
                if (bFlag) break;
            }
            return m_mi;
        }

        private void DrawBezier(Graphics g, Pen p, PointF ptStart, PointF ptEnd, float f) {
            this.DrawBezier(g, p, ptStart.X, ptStart.Y, ptEnd.X, ptEnd.Y, f);
        }

        private void DrawBezier(Graphics g, Pen p, float x1, float y1, float x2, float y2, float f) {
            float n = (Math.Abs(x1 - x2) * f);
            if (this._Curvature != 0 && n < 30) n = 30;
            g.DrawBezier(p,
                x1, y1,
                x1 + n, y1,
                x2 - n, y2,
                x2, y2);
        }

        private GraphicsPath CreateBezierPath(float x1, float y1, float x2, float y2, float f) {
            GraphicsPath gp = new GraphicsPath();
            float n = (Math.Abs(x1 - x2) * f);
            if (this._Curvature != 0 && n < 30) n = 30;
            gp.AddBezier(
                x1, y1,
                x1 + n, y1,
                x2 - n, y2,
                x2, y2
                );
            return gp;
        }

        private void RenderBorder(Graphics g, Rectangle rect, Image img) {
            //Fill the four corners
            g.DrawImage(img, new Rectangle(rect.X - 5, rect.Y - 5, 5, 5),
                new Rectangle(0, 0, 5, 5), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(rect.Right, rect.Y - 5, 5, 5),
                new Rectangle(img.Width - 5, 0, 5, 5), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(rect.X - 5, rect.Bottom, 5, 5),
                new Rectangle(0, img.Height - 5, 5, 5), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(rect.Right, rect.Bottom, 5, 5),
                new Rectangle(img.Width - 5, img.Height - 5, 5, 5), GraphicsUnit.Pixel);
            //four sides
            g.DrawImage(img, new Rectangle(rect.X - 5, rect.Y, 5, rect.Height),
                new Rectangle(0, 5, 5, img.Height - 10), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(rect.X, rect.Y - 5, rect.Width, 5),
                new Rectangle(5, 0, img.Width - 10, 5), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(rect.Right, rect.Y, 5, rect.Height),
                new Rectangle(img.Width - 5, 5, 5, img.Height - 10), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(rect.X, rect.Bottom, rect.Width, 5),
                new Rectangle(5, img.Height - 5, img.Width - 10, 5), GraphicsUnit.Pixel);
        }

        #endregion private

        #region public --------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Find by canvas coordinates.
        /// </summary>
        /// <param name="pt">Coordinates in the canvas</param>
        /// <returns>Data found</returns>
        public NodeFindInfo FindNodeFromPoint(PointF pt) {
            m_find.Node = null; m_find.NodeOption = null; m_find.Mark = null;
            for (int i = this._Nodes.Count - 1; i >= 0; i--) {
                if (!string.IsNullOrEmpty(this._Nodes[i].Mark) && this.PointInRectangle(this._Nodes[i].MarkRectangle, pt.X, pt.Y)) {
                    m_find.Mark = this._Nodes[i].Mark;
                    m_find.MarkLines = this._Nodes[i].MarkLines;
                    return m_find;
                }
                foreach (STNodeOption v in this._Nodes[i].InputOptions) {
                    if (v == STNodeOption.Empty) continue;
                    if (this.PointInRectangle(v.HitRectangle, pt.X, pt.Y)) m_find.NodeOption = v;
                }
                foreach (STNodeOption v in this._Nodes[i].OutputOptions) {
                    if (v == STNodeOption.Empty) continue;
                    if (this.PointInRectangle(v.HitRectangle, pt.X, pt.Y)) m_find.NodeOption = v;
                }
                if (this.PointInRectangle(this._Nodes[i].Rectangle, pt.X, pt.Y)) {
                    m_find.Node = this._Nodes[i];
                }

                if (m_find.NodeOption != null && m_find.Node != null)
                {
                    // If we hit a node and it's option, the option takes
                    // precedence so clear the active control.
                    m_find.Node.ClearActiveCtrl();
                }

                if (m_find.NodeOption != null || m_find.Node != null) return m_find;
            }
            return m_find;
        }

        /// <summary>
        /// Returns the NodeFindInfo which was filled last time FindNodeFromPoint() was called.
        /// </summary>
        /// <returns></returns>
        public NodeFindInfo GetPreviousNodeFindInfo()
        {
            return m_find;
        }

        /// <summary>
        /// Get the selected Node collection.
        /// </summary>
        /// <returns>Node collection</returns>
        public STNode[] GetSelectedNode() {
            return m_hs_node_selected.ToArray();
        }
        /// <summary>
        /// Convert canvas coordinates to control coordinates.
        /// </summary>
        /// <param name="number">parameter</param>
        /// <param name="isX">Is it the X coordinate</param>
        /// <returns>Converted coordinates</returns>
        public float CanvasToControl(float number, bool isX) {
            return (number * this._CanvasScale) + (isX ? this._CanvasOffsetX : this._CanvasOffsetY);
        }
        /// <summary>
        /// Convert canvas coordinates to control coordinates.
        /// </summary>
        /// <param name="pt">coordinate</param>
        /// <returns>Converted coordinates</returns>
        public PointF CanvasToControl(PointF pt) {
            pt.X = (pt.X * this._CanvasScale) + this._CanvasOffsetX;
            pt.Y = (pt.Y * this._CanvasScale) + this._CanvasOffsetY;
            //pt.X += this._CanvasOffsetX;
            //pt.Y += this._CanvasOffsetY;
            return pt;
        }
        /// <summary>
        /// Convert canvas coordinates to control coordinates.
        /// </summary>
        /// <param name="pt">coordinate</param>
        /// <returns>Converted coordinates</returns>
        public Point CanvasToControl(Point pt) {
            pt.X = (int)(pt.X * this._CanvasScale + this._CanvasOffsetX);
            pt.Y = (int)(pt.Y * this._CanvasScale + this._CanvasOffsetY);
            //pt.X += (int)this._CanvasOffsetX;
            //pt.Y += (int)this._CanvasOffsetY;
            return pt;
        }
        /// <summary>
        /// Convert canvas coordinates to control coordinates.
        /// </summary>
        /// <param name="rect">Rectangular area</param>
        /// <returns>Converted rectangular area</returns>
        public Rectangle CanvasToControl(Rectangle rect) {
            rect.X = (int)((rect.X * this._CanvasScale) + this._CanvasOffsetX);
            rect.Y = (int)((rect.Y * this._CanvasScale) + this._CanvasOffsetY);
            rect.Width = (int)(rect.Width * this._CanvasScale);
            rect.Height = (int)(rect.Height * this._CanvasScale);
            //rect.X += (int)this._CanvasOffsetX;
            //rect.Y += (int)this._CanvasOffsetY;
            return rect;
        }
        /// <summary>
        /// Convert canvas coordinates to control coordinates.
        /// </summary>
        /// <param name="rect">Rectangular area</param>
        /// <returns>Converted rectangular area</returns>
        public RectangleF CanvasToControl(RectangleF rect) {
            rect.X = (rect.X * this._CanvasScale) + this._CanvasOffsetX;
            rect.Y = (rect.Y * this._CanvasScale) + this._CanvasOffsetY;
            rect.Width = (rect.Width * this._CanvasScale);
            rect.Height = (rect.Height * this._CanvasScale);
            //rect.X += this._CanvasOffsetX;
            //rect.Y += this._CanvasOffsetY;
            return rect;
        }
        /// <summary>
        /// Convert control coordinates to canvas coordinates.
        /// </summary>
        /// <param name="number">parameter</param>
        /// <param name="isX">Is it the X coordinate</param>
        /// <returns>Converted coordinates</returns>
        public float ControlToCanvas(float number, bool isX) {
            return (number - (isX ? this._CanvasOffsetX : this._CanvasOffsetY)) / this._CanvasScale;
        }
        /// <summary>
        /// Convert control coordinates to canvas coordinates.
        /// </summary>
        /// <param name="pt">coordinate</param>
        /// <returns>Converted coordinates</returns>
        public Point ControlToCanvas(Point pt) {
            pt.X = (int)((pt.X - this._CanvasOffsetX) / this._CanvasScale);
            pt.Y = (int)((pt.Y - this._CanvasOffsetY) / this._CanvasScale);
            return pt;
        }
        /// <summary>
        /// Convert control coordinates to canvas coordinates.
        /// </summary>
        /// <param name="pt">coordinate</param>
        /// <returns>Converted coordinates</returns>
        public PointF ControlToCanvas(PointF pt) {
            pt.X = ((pt.X - this._CanvasOffsetX) / this._CanvasScale);
            pt.Y = ((pt.Y - this._CanvasOffsetY) / this._CanvasScale);
            return pt;
        }
        /// <summary>
        /// Convert control coordinates to canvas coordinates.
        /// </summary>
        /// <param name="rect">Rectangular area</param>
        /// <returns>Converted area</returns>
        public Rectangle ControlToCanvas(Rectangle rect) {
            rect.X = (int)((rect.X - this._CanvasOffsetX) / this._CanvasScale);
            rect.Y = (int)((rect.Y - this._CanvasOffsetY) / this._CanvasScale);
            rect.Width = (int)(rect.Width / this._CanvasScale);
            rect.Height = (int)(rect.Height / this._CanvasScale);
            return rect;
        }
        /// <summary>
        /// Convert control coordinates to canvas coordinates.
        /// </summary>
        /// <param name="rect">Rectangular area</param>
        /// <returns>Converted area</returns>
        public RectangleF ControlToCanvas(RectangleF rect) {
            rect.X = ((rect.X - this._CanvasOffsetX) / this._CanvasScale);
            rect.Y = ((rect.Y - this._CanvasOffsetY) / this._CanvasScale);
            rect.Width = (rect.Width / this._CanvasScale);
            rect.Height = (rect.Height / this._CanvasScale);
            return rect;
        }
        /// <summary>
        /// Move the coordinates of the origin of the canvas to the specified coordinate position of the control.
        /// Cannot move when Node does not exist.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="bAnimation">Whether to start the animation effect during the movement</param>
        /// <param name="ma">Specify the coordinate parameters that need to be modified</param>
        public void MoveCanvas(float x, float y, bool bAnimation, CanvasMoveArgs ma) {
            if (this._Nodes.Count == 0) {
                m_real_canvas_x = m_real_canvas_y = 10;
                return;
            }
            int l = (int)((this._CanvasValidBounds.Left + 50) * this._CanvasScale);
            int t = (int)((this._CanvasValidBounds.Top + 50) * this._CanvasScale);
            int r = (int)((this._CanvasValidBounds.Right - 50) * this._CanvasScale);
            int b = (int)((this._CanvasValidBounds.Bottom - 50) * this._CanvasScale);
            if (r + x < 0) x = -r;
            if (this.Width - l < x) x = this.Width - l;
            if (b + y < 0) y = -b;
            if (this.Height - t < y) y = this.Height - t;
            if (bAnimation) {
                if ((ma & CanvasMoveArgs.Left) == CanvasMoveArgs.Left)
                    m_real_canvas_x = x;
                if ((ma & CanvasMoveArgs.Top) == CanvasMoveArgs.Top)
                    m_real_canvas_y = y;
            } else {
                m_real_canvas_x = this._CanvasOffsetX = x;
                m_real_canvas_y = this._CanvasOffsetY = y;
            }
            this.OnCanvasMoved(EventArgs.Empty);
        }
        /// <summary>
        /// Zoom canvas.
        /// Unable to zoom when there is no Node.
        /// </summary>
        /// <param name="f">scaling ratio</param>
        /// <param name="x">The coordinate of the zoom center X on the control</param>
        /// <param name="y">The coordinate of the zoom center Y on the control</param>
        public void ScaleCanvas(float f, float x, float y) {
            if (this._Nodes.Count == 0) {
                this._CanvasScale = 1F;
                return;
            }
            if (this._CanvasScale == f) return;

            const float min = 0.2f;
            const float max = 2.5f;

            if (f < min) f = min; else if (f > max) f = max;
            float x_c = this.ControlToCanvas(x, true);
            float y_c = this.ControlToCanvas(y, false);
            this._CanvasScale = f;
            this._CanvasOffsetX = m_real_canvas_x -= this.CanvasToControl(x_c, true) - x;
            this._CanvasOffsetY = m_real_canvas_y -= this.CanvasToControl(y_c, false) - y;
            this.OnCanvasZoomed(EventArgs.Empty);
            this.Invalidate();
        }

        // Commented out GetConnectionInfo() below since it seems to be lazily populated and
        // generally not very trustworthy.

        /// <summary>
        /// Get the corresponding relationship of the currently connected Option.
        /// </summary>
        /// <returns>Connection information collection</returns>
        //public ConnectionInfo[] GetConnectionInfo() {
        //    return m_dic_gp_info.Values.ToArray();
        //}

        /// <summary>
        /// Determine whether there is a connection path between two Nodes.
        /// </summary>
        /// <param name="nodeStart">Starting Node</param>
        /// <param name="nodeFind">Target Node</param>
        /// <returns>Return true if there is a path, otherwise false</returns>
        public static bool CanFindNodePath(STNode nodeStart, STNode nodeFind) {
            HashSet<STNode> hs = new HashSet<STNode>();
            return STNodeEditor.CanFindNodePath(nodeStart, nodeFind, hs);
        }
        private static bool CanFindNodePath(STNode nodeStart, STNode nodeFind, HashSet<STNode> hs) {
            foreach (STNodeOption op_1 in nodeStart.OutputOptions) {
                foreach (STNodeOption op_2 in op_1.ConnectedOption) {
                    if (op_2.Owner == nodeFind) return true;
                    if (hs.Add(op_2.Owner)) {
                        if (STNodeEditor.CanFindNodePath(op_2.Owner, nodeFind)) return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Get the image of the specified rectangular area in the canvas.
        /// </summary>
        /// <param name="rect">A specified rectangular area in the canvas</param>
        /// <returns>image</returns>
        public Image GetCanvasImage(Rectangle rect) { return this.GetCanvasImage(rect, 1f); }
        /// <summary>
        /// Get the image of the specified rectangular area in the canvas.
        /// </summary>
        /// <param name="rect">A specified rectangular area in the canvas</param>
        /// <param name="fScale">scaling ratio</param>
        /// <returns>image</returns>
        public Image GetCanvasImage(Rectangle rect, float fScale) {
            if (fScale < 0.5) fScale = 0.5f; else if (fScale > 3) fScale = 3;
            Image img = new Bitmap((int)(rect.Width * fScale), (int)(rect.Height * fScale));
            using (Graphics g = Graphics.FromImage(img)) {
                g.Clear(this.BackColor);
                g.ScaleTransform(fScale, fScale);
                m_drawing_tools.Graphics = g;

                if (this._ShowGrid) this.OnDrawGrid(m_drawing_tools, rect.Width, rect.Height);
                g.TranslateTransform(-rect.X, -rect.Y); //Move the coordinate system
                this.OnDrawNode(m_drawing_tools, rect);
                this.OnDrawConnectedLine(m_drawing_tools);

                g.ResetTransform();

                if (this._ShowLocation) this.OnDrawNodeOutLocation(m_drawing_tools, img.Size, m_lst_node_out);
            }
            return img;
        }
        /// <summary>
        /// Save the class content in the canvas to the file.
        /// </summary>
        /// <param name="strFileName">file path</param>
        public void SaveCanvas(string strFileName) {
            using (FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write)) {
                this.SaveCanvas(fs);
            }
        }
        /// <summary>
        /// Save the class content in the canvas to the data stream.
        /// </summary>
        /// <param name="s">Data stream object</param>
        public void SaveCanvas(Stream s) {
            Dictionary<STNodeOption, long> dic = new Dictionary<STNodeOption, long>();
            s.Write(new byte[] { (byte)'S', (byte)'T', (byte)'N', (byte)'D' }, 0, 4); //file head
            s.WriteByte(1);                                                           //ver
            using (GZipStream gs = new GZipStream(s, CompressionMode.Compress)) {
                gs.Write(BitConverter.GetBytes(this._CanvasOffsetX), 0, 4);
                gs.Write(BitConverter.GetBytes(this._CanvasOffsetY), 0, 4);
                gs.Write(BitConverter.GetBytes(this._CanvasScale), 0, 4);
                gs.Write(BitConverter.GetBytes(this._Nodes.Count), 0, 4);
                foreach (STNode node in this._Nodes) {
                    try {
                        byte[] byNode = node.GetSaveData();
                        gs.Write(BitConverter.GetBytes(byNode.Length), 0, 4);
                        gs.Write(byNode, 0, byNode.Length);
                        foreach (STNodeOption op in node.InputOptions) if (!dic.ContainsKey(op)) dic.Add(op, dic.Count);
                        foreach (STNodeOption op in node.OutputOptions) if (!dic.ContainsKey(op)) dic.Add(op, dic.Count);
                    } catch (Exception ex) {
                        throw new Exception("Error getting node data -" + node.Title, ex);
                    }
                }
                gs.Write(BitConverter.GetBytes(m_dic_gp_info.Count), 0, 4);
                foreach (var v in m_dic_gp_info.Values)
                    gs.Write(BitConverter.GetBytes(((dic[v.Output] << 32) | dic[v.Input])), 0, 8);
            }
        }
        /// <summary>
        /// Get the binary data of the content in the canvas.
        /// </summary>
        /// <returns>Binary data</returns>
        public byte[] GetCanvasData() {
            using (MemoryStream ms = new MemoryStream()) {
                this.SaveCanvas(ms);
                return ms.ToArray();
            }
        }
        /// <summary>
        /// Load assembly.
        /// </summary>
        /// <param name="strFiles">Assembly collection</param>
        /// <returns>The number of files of type STNode</returns>
        public int LoadAssembly(string[] strFiles) {
            int nCount = 0;
            foreach (var v in strFiles) {
                try {
                    if (this.LoadAssembly(v)) nCount++;
                } catch { }
            }
            return nCount;
        }
        /// <summary>
        /// Load assembly.
        /// </summary>
        /// <param name="strFile">Specify the file to be loaded</param>
        /// <returns>Whether the load is successful</returns>
        public bool LoadAssembly(string strFile) {
            bool bFound = false;
            Assembly asm = Assembly.LoadFrom(strFile);
            if (asm == null) return false;
            foreach (var t in asm.GetTypes()) {
                if (t.IsAbstract) continue;
                if (t == m_type_node || t.IsSubclassOf(m_type_node)) {
                    if (m_dic_type.ContainsKey(t.GUID.ToString())) continue;
                    m_dic_type.Add(t.GUID.ToString(), t);
                    bFound = true;
                }
            }
            return bFound;
        }
        /// <summary>
        /// Get the Node type loaded in the current editor.
        /// </summary>
        /// <returns>Type collection</returns>
        public Type[] GetTypes() {
            return m_dic_type.Values.ToArray();
        }
        /// <summary>
        /// Load data from file.
        /// Note: This method does not clear the data in the canvas, but data overlay.
        /// </summary>
        /// <param name="strFileName">file path</param>
        public void LoadCanvas(string strFileName) {
            using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(strFileName)))
                this.LoadCanvas(ms);
        }
        /// <summary>
        /// Load data from binary.
        /// Note: This method does not clear the data in the canvas, but data overlay.
        /// </summary>
        /// <param name="byData">Binary data</param>
        public void LoadCanvas(byte[] byData) {
            using (MemoryStream ms = new MemoryStream(byData))
                this.LoadCanvas(ms);
        }
        /// <summary>
        /// Load data from the data stream.
        /// Note: This method does not clear the data in the canvas, but data overlay.
        /// </summary>
        /// <param name="s">Data stream object</param>
        public void LoadCanvas(Stream s) {
            int nLen = 0;
            byte[] byLen = new byte[4];
            s.Read(byLen, 0, 4);
            if (BitConverter.ToInt32(byLen, 0) != BitConverter.ToInt32(new byte[] { (byte)'S', (byte)'T', (byte)'N', (byte)'D' }, 0))
                throw new InvalidDataException("Unrecognized file type.");
            if (s.ReadByte() != 1) throw new InvalidDataException("Unrecognized file version number.");
            using (GZipStream gs = new GZipStream(s, CompressionMode.Decompress)) {
                gs.Read(byLen, 0, 4);
                float x = BitConverter.ToSingle(byLen, 0);
                gs.Read(byLen, 0, 4);
                float y = BitConverter.ToSingle(byLen, 0);
                gs.Read(byLen, 0, 4);
                float scale = BitConverter.ToSingle(byLen, 0);
                gs.Read(byLen, 0, 4);
                int nCount = BitConverter.ToInt32(byLen, 0);
                Dictionary<long, STNodeOption> dic = new Dictionary<long, STNodeOption>();
                HashSet<STNodeOption> hs = new HashSet<STNodeOption>();
                byte[] byData = null;
                for (int i = 0; i < nCount; i++) {
                    gs.Read(byLen, 0, byLen.Length);
                    nLen = BitConverter.ToInt32(byLen, 0);
                    byData = new byte[nLen];
                    gs.Read(byData, 0, byData.Length);
                    STNode node = null;
                    try { node = this.GetNodeFromData(byData); } catch (Exception ex) {
                        throw new Exception("An error occurred while loading the node, the data may be corrupted\r\n" + ex.Message, ex);
                    }
                    try { this._Nodes.Add(node); } catch (Exception ex) {
                        throw new Exception("Error loading node -" + node.Title, ex);
                    }
                    foreach (STNodeOption op in node.InputOptions) if (hs.Add(op)) dic.Add(dic.Count, op);
                    foreach (STNodeOption op in node.OutputOptions) if (hs.Add(op)) dic.Add(dic.Count, op);
                }
                gs.Read(byLen, 0, 4);
                nCount = BitConverter.ToInt32(byLen, 0);
                byData = new byte[8];
                for (int i = 0; i < nCount; i++) {
                    gs.Read(byData, 0, byData.Length);
                    long id = BitConverter.ToInt64(byData, 0);
                    long op_out = id >> 32;
                    long op_in = (int)id;
                    dic[op_out].ConnectOption(dic[op_in]);
                }
                this.ScaleCanvas(scale, 0, 0);
                this.MoveCanvas(x, y, false, CanvasMoveArgs.All);
            }
            this.BuildBounds();
            foreach (STNode node in this._Nodes) node.OnEditorLoadCompleted();
        }

        private STNode GetNodeFromData(byte[] byData) {
            int nIndex = 0;
            string strModel = Encoding.UTF8.GetString(byData, nIndex + 1, byData[nIndex]);
            nIndex += byData[nIndex] + 1;
            string strGUID = Encoding.UTF8.GetString(byData, nIndex + 1, byData[nIndex]);
            nIndex += byData[nIndex] + 1;

            int nLen = 0;

            Dictionary<string, byte[]> dic = new Dictionary<string, byte[]>();
            while (nIndex < byData.Length) {
                nLen = BitConverter.ToInt32(byData, nIndex);
                nIndex += 4;
                string strKey = Encoding.UTF8.GetString(byData, nIndex, nLen);
                nIndex += nLen;
                nLen = BitConverter.ToInt32(byData, nIndex);
                nIndex += 4;
                byte[] byValue = new byte[nLen];
                Array.Copy(byData, nIndex, byValue, 0, nLen);
                nIndex += nLen;
                dic.Add(strKey, byValue);
            }
            if (!m_dic_type.ContainsKey(strGUID))
            {
                throw new TypeLoadException(
                    "Cannot find the assembly where the type {" + strModel.Split('|')[1] + 
                    "} is located. Ensure that the assembly {" + strModel.Split('|')[0] + 
                    "} has been loaded correctly by the editor. You can load the assembly by calling LoadAssembly().");
            }
            Type t = m_dic_type[strGUID];
            STNode node = (STNode)Activator.CreateInstance(t);
            node.OnLoadNode(dic);
            return node;
        }
        /// <summary>
        /// Display prompt information in the canvas.
        /// </summary>
        /// <param name="strText">Information to be displayed</param>
        /// <param name="foreColor">Information foreground</param>
        /// <param name="backColor">Information background color</param>
        public void ShowAlert(string strText, Color foreColor, Color backColor) {
            this.ShowAlert(strText, foreColor, backColor, 1000, AlertLocation.RightBottom, true);
        }
        /// <summary>
        /// Display prompt information in the canvas.
        /// </summary>
        /// <param name="strText">Information to be displayed</param>
        /// <param name="foreColor">Information foreground</param>
        /// <param name="backColor">Information background color</param>
        /// <param name="al">Where to display the information</param>
        public void ShowAlert(string strText, Color foreColor, Color backColor, AlertLocation al) {
            this.ShowAlert(strText, foreColor, backColor, 1000, al, true);
        }
        /// <summary>
        /// Display prompt information in the canvas.
        /// </summary>
        /// <param name="strText">Information to be displayed</param>
        /// <param name="foreColor">Information foreground</param>
        /// <param name="backColor">Information background color</param>
        /// <param name="nTime">Message duration</param>
        /// <param name="al">Where to display the information</param>
        /// <param name="bRedraw">Whether to redraw immediately</param>
        public void ShowAlert(string strText, Color foreColor, Color backColor, int nTime, AlertLocation al, bool bRedraw) {
            m_str_alert = strText;
            m_forecolor_alert = foreColor;
            m_backcolor_alert = backColor;
            m_time_alert = nTime;
            m_dt_alert = DateTime.Now;
            m_alpha_alert = 255;
            m_al = al;
            if (bRedraw) this.Invalidate();
        }
        /// <summary>
        /// Set the active node in the canvas.
        /// </summary>
        /// <param name="node">Need to be set as active node</param>
        /// <returns>Active node before setting</returns>
        public STNode SetActiveNode(STNode node) {
            if (node != null && !this._Nodes.Contains(node)) return this._ActiveNode;
            STNode ret = this._ActiveNode;
            if (this._ActiveNode != node) {         //Reset active selection node
                if (node != null) {
                    this._Nodes.MoveToEnd(node);
                    node.IsActive = true;
                    node.SetSelected(true, false);
                    node.OnGotFocus(EventArgs.Empty);
                }
                if (this._ActiveNode != null) {
                    this._ActiveNode.IsActive /*= this._ActiveNode.IsSelected*/ = false;
                    this._ActiveNode.OnLostFocus(EventArgs.Empty);
                }
                this._ActiveNode = node;
                this.Invalidate();
                this.OnActiveChanged(EventArgs.Empty);
                //this.OnSelectedChanged(EventArgs.Empty);
            }
            return ret;
        }
        /// <summary>
        /// Add a selected node to the canvas
        /// </summary>
        /// <param name="node">The node that needs to be selected</param>
        /// <returns>Is it added successfully?</returns>
        public bool AddSelectedNode(STNode node) {
            if (!this._Nodes.Contains(node)) return false;
            bool b = !node.IsSelected;
            node.IsSelected = true;
            lock (m_hs_node_selected) return m_hs_node_selected.Add(node) || b;
        }
        /// <summary>
        /// Remove a selected node from the canvas.
        /// </summary>
        /// <param name="node">The node that needs to be removed</param>
        /// <returns>Is the removal successful?</returns>
        public bool RemoveSelectedNode(STNode node) {
            if (!this._Nodes.Contains(node)) return false;
            bool b = node.IsSelected;
            node.IsSelected = false;
            lock (m_hs_node_selected) return m_hs_node_selected.Remove(node) || b;
        }
        /// <summary>
        /// Add the default data type color to the editor
        /// </summary>
        /// <param name="t">type of data</param>
        /// <param name="clr">Corresponding color</param>
        /// <returns>Color after being set</returns>
        public Color SetTypeColor(Type t, Color clr) {
            return this.SetTypeColor(t, clr, false);
        }
        /// <summary>
        /// Add the default data type color to the editor
        /// </summary>
        /// <param name="t">type of data</param>
        /// <param name="clr">Corresponding color</param>
        /// <param name="bReplace">Whether to replace the color if it already exists</param>
        /// <returns>Color after being set</returns>
        public Color SetTypeColor(Type t, Color clr, bool bReplace) {
            if (this._TypeColor.ContainsKey(t)) {
                if (bReplace) this._TypeColor[t] = clr;
            } else {
                this._TypeColor.Add(t, clr);
            }
            return this._TypeColor[t];
        }

        #endregion public
    }
}
