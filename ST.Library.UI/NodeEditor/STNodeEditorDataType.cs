using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;

namespace ST.Library.UI.NodeEditor
{
    public enum ConnectionStatus
    {
        /// <summary>
        /// No owner
        /// </summary>
        [Description("No owner")]
        NoOwner,
        /// <summary>
        /// Same owner
        /// </summary>
        [Description("Same owner")]
        SameOwner,
        /// <summary>
        /// Both are input or output options
        /// </summary>
        [Description("Both are input or output options")]
        SameInputOrOutput,
        /// <summary>
        /// Different data types
        /// </summary>
        [Description("Different data types")]
        ErrorType,
        /// <summary>
        /// Single connection node
        /// </summary>
        [Description("Single connection node")]
        SingleOption,
        /// <summary>
        /// Circular path
        /// </summary>
        [Description("Circular path")]
        Loop,
        /// <summary>
        /// Existing connection
        /// </summary>
        [Description("Existing connection")]
        Exists,
        /// <summary>
        /// Empty option
        /// </summary>
        [Description("Empty option")]
        EmptyOption,
        /// <summary>
        /// Already connected
        /// </summary>
        [Description("Connected")]
        Connected,
        /// <summary>
        /// Disconnected
        /// </summary>
        [Description("Disconnected")]
        Disconnected,
        /// <summary>
        /// Node is locked
        /// </summary>
        [Description("Node is locked")]
        Locked,
        /// <summary>
        /// Operation denied
        /// </summary>
        [Description("Operation denied")]
        Reject,
        /// <summary>
        /// Connecting
        /// </summary>
        [Description("Connecting")]
        Connecting,
        /// <summary>
        /// Disconnecting
        /// </summary>
        [Description("Disconnecting")]
        Disconnecting
    }

    public enum AlertLocation
    {
        Left,
        Top,
        Right,
        Bottom,
        Center,
        LeftTop,
        RightTop,
        RightBottom,
        LeftBottom,
    }

    public struct DrawingTools
    {
        public Graphics Graphics;
        public Pen Pen;
        public SolidBrush SolidBrush;
    }

    public enum CanvasMoveArgs      //Parameters required when moving the canvas View->MoveCanvas()
    {
        Left = 1,                   //Means to move only the X coordinate
        Top = 2,                    //Means to move only the Y coordinate
        All = 4                     //Means X Y moves at the same time
    }

    public struct NodeFindInfo
    {
        public STNode Node;
        public STNodeOption NodeOption;
        public string Mark;
        public string[] MarkLines;
    }

    public struct ConnectionInfo
    {
        public STNodeOption Input;
        public STNodeOption Output;
    }

    public delegate void STNodeOptionEventHandler(object sender, STNodeOptionEventArgs e);

    public class STNodeOptionEventArgs : EventArgs
    {
        private STNodeOption _TargetOption;
        /// <summary>
        /// The corresponding Option that triggered this event.
        /// </summary>
        public STNodeOption TargetOption {
            get { return _TargetOption; }
        }

        private ConnectionStatus _Status;
        /// <summary>
        /// Connection status between options.
        /// </summary>
        public ConnectionStatus Status {
            get { return _Status; }
            internal set { _Status = value; }
        }

        private bool _IsSponsor;
        /// <summary>
        /// Is it the initiator of this behavior?
        /// </summary>
        public bool IsSponsor {
            get { return _IsSponsor; }
        }

        public STNodeOptionEventArgs(bool isSponsor, STNodeOption opTarget, ConnectionStatus cr) {
            this._IsSponsor = isSponsor;
            this._TargetOption = opTarget;
            this._Status = cr;
        }
    }

    public delegate void STNodeEditorEventHandler(object sender, STNodeEditorEventArgs e);
    public delegate void STNodeEditorOptionEventHandler(object sender, STNodeEditorOptionEventArgs e);


    public class STNodeEditorEventArgs : EventArgs
    {
        private STNode _Node;

        public STNode Node {
            get { return _Node; }
        }

        public STNodeEditorEventArgs(STNode node) {
            this._Node = node;
        }
    }

    public class STNodeEditorOptionEventArgs : STNodeOptionEventArgs
    {

        private STNodeOption _CurrentOption;
        /// <summary>
        /// Option that triggers the event actively.
        /// </summary>
        public STNodeOption CurrentOption {
            get { return _CurrentOption; }
        }

        private bool _Continue = true;
        /// <summary>
        /// Whether to continue downward operation Used for Begin (Connecting/Disconnecting) whether to continue backward operation.
        /// </summary>
        public bool Continue {
            get { return _Continue; }
            set { _Continue = value; }
        }

        public STNodeEditorOptionEventArgs(STNodeOption opTarget, STNodeOption opCurrent, ConnectionStatus cr)
            : base(false, opTarget, cr) {
            this._CurrentOption = opCurrent;
        }
    }

    public struct NodeMovement
    {
        public STNode Node;
        public Point OldLocation;
        public Point NewLocation;
    }

    public class STNodesMovedEventArgs : EventArgs
    {
        public STNodesMovedEventArgs(NodeMovement[] movements)
        {
            mMovements = movements;
        }

        public NodeMovement[] Movements
        {
            get { return mMovements; }
        }

        private NodeMovement[] mMovements;
    }

    public delegate void STNodesMovedEventHandler(object sender, STNodesMovedEventArgs e);
}
