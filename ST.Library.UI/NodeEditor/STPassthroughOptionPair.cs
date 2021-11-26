using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ST.Library.UI.NodeEditor
{
    // This class was added for the Basis editors.
    // It contains a simplified version of the STNodeHub logic
    // and has one input/output option pair which can be connected
    // to any type. An instance of this class can be added to an STNode
    // class and any additional "normal" options can then be added.
    public class STPassthroughOptionPair
    {
        private STNode mTargetNode;
        private bool m_bSingle;
        private string m_strIn;
        private string m_strOut;

        private STPassthroughOption mValueInOption;
        private STPassthroughOption mValueOutOption;

        public STPassthroughOptionPair(STNode targetNode, bool bSingle, string strTextIn, string strTextOut)
        {
            mTargetNode = targetNode;
            m_bSingle = bSingle;
            m_strIn = strTextIn;
            m_strOut = strTextOut;
        }

        public void CreateOptionPair()
        {
            mValueInOption = new STPassthroughOption(m_strIn, typeof(object), m_bSingle);
            mValueOutOption = new STPassthroughOption(m_strOut, typeof(object), false);
            mTargetNode.InputOptions.Add(mValueInOption);
            mTargetNode.OutputOptions.Add(mValueOutOption);
            mValueInOption.Connected += new STNodeOptionEventHandler(input_Connected);
            mValueInOption.DataTransfer += new STNodeOptionEventHandler(input_DataTransfer);
            mValueInOption.Disconnected += new STNodeOptionEventHandler(input_Disconnected);
            mValueOutOption.Connected += new STNodeOptionEventHandler(output_Connected);
            mValueOutOption.Disconnected += new STNodeOptionEventHandler(output_Disconnected);
        }

        //protected override void OnOwnerChanged()
        //{
        //    base.OnOwnerChanged();
        //    if (this.Owner == null) return;
        //    using (Graphics g = this.Owner.CreateGraphics())
        //    {
        //        this.Width = base.GetDefaultNodeSize(g).Width;
        //    }
        //}

        //private void Addhub()
        //{
        //    var input = new STPassthroughOption(m_strIn, typeof(object), m_bSingle);
        //    var output = new STPassthroughOption(m_strOut, typeof(object), false);
        //    this.InputOptions.Add(input);
        //    this.OutputOptions.Add(output);
        //    input.Connected += new STNodeOptionEventHandler(input_Connected);
        //    input.DataTransfer += new STNodeOptionEventHandler(input_DataTransfer);
        //    input.Disconnected += new STNodeOptionEventHandler(input_Disconnected);
        //    output.Connected += new STNodeOptionEventHandler(output_Connected);
        //    output.Disconnected += new STNodeOptionEventHandler(output_Disconnected);
        //    this.Height = this.TitleHeight + this.InputOptions.Count * 20;
        //}

        void output_Disconnected(object sender, STNodeOptionEventArgs e)
        {
            //STNodeOption op = sender as STNodeOption;
            //if (op.ConnectionCount != 0) return;
            //int nIndex = this.OutputOptions.IndexOf(op);
            //if (this.InputOptions[nIndex].ConnectionCount != 0) return;
            //this.InputOptions.RemoveAt(nIndex);
            //this.OutputOptions.RemoveAt(nIndex);
            //if (this.Owner != null) this.Owner.BuildLinePath();
            //this.Height -= 20;

            if (mValueInOption.ConnectionCount == 0 && mValueOutOption.ConnectionCount == 0)
            {
                mValueInOption.DataType = typeof(object);
                mValueOutOption.DataType = typeof(object);
            }
        }

        void output_Connected(object sender, STNodeOptionEventArgs e)
        {
            STNodeOption op = sender as STNodeOption;
            int nIndex = mTargetNode.OutputOptions.IndexOf(op);
            var t = typeof(object);
            if (mTargetNode.InputOptions[nIndex].DataType == t)
            {
                op.DataType = e.TargetOption.DataType;
                mTargetNode.InputOptions[nIndex].DataType = op.DataType;
                foreach (STNodeOption v in mTargetNode.InputOptions)
                {
                    if (v.DataType == t) return;
                }
                //this.Addhub();
            }
        }

        void input_Disconnected(object sender, STNodeOptionEventArgs e)
        {
            //STNodeOption op = sender as STNodeOption;
            //if (op.ConnectionCount != 0) return;
            //int nIndex = this.InputOptions.IndexOf(op);
            //if (this.OutputOptions[nIndex].ConnectionCount != 0) return;
            //this.InputOptions.RemoveAt(nIndex);
            //this.OutputOptions.RemoveAt(nIndex);
            //if (this.Owner != null) this.Owner.BuildLinePath();
            //this.Height -= 20;

            if (mValueInOption.ConnectionCount == 0 && mValueOutOption.ConnectionCount == 0)
            {
                mValueInOption.DataType = typeof(object);
                mValueOutOption.DataType = typeof(object);
            }
        }

        void input_DataTransfer(object sender, STNodeOptionEventArgs e)
        {
            STNodeOption op = sender as STNodeOption;
            int nIndex = mTargetNode.InputOptions.IndexOf(op);
            if (e.Status != ConnectionStatus.Connected)
                mTargetNode.OutputOptions[nIndex].Data = null;
            else
                mTargetNode.OutputOptions[nIndex].Data = e.TargetOption.Data;
            mTargetNode.OutputOptions[nIndex].TransferData();
        }

        void input_Connected(object sender, STNodeOptionEventArgs e)
        {
            STNodeOption op = sender as STNodeOption;
            int nIndex = mTargetNode.InputOptions.IndexOf(op);
            var t = typeof(object);
            if (op.DataType == t)
            {
                op.DataType = e.TargetOption.DataType;
                mTargetNode.OutputOptions[nIndex].DataType = op.DataType;
                foreach (STNodeOption v in mTargetNode.InputOptions)
                {
                    if (v.DataType == t) return;
                }
                //this.Addhub();
            }
            else
            {
                //this.OutputOptions[nIndex].Data = e.TargetOption.Data;
                mTargetNode.OutputOptions[nIndex].TransferData(e.TargetOption.Data);
            }
        }

        public class STPassthroughOption : STNodeOption
        {
            public STPassthroughOption(string strText, Type dataType, bool bSingle) : base(strText, dataType, bSingle) { }

            public override ConnectionStatus ConnectOption(STNodeOption op)
            {
                var t = typeof(object);
                if (this.DataType != t) return base.ConnectOption(op);
                this.DataType = op.DataType;
                var ret = base.ConnectOption(op);
                if (ret != ConnectionStatus.Connected) this.DataType = t;
                return ret;
            }

            public override ConnectionStatus CanConnect(STNodeOption op)
            {
                if (op == STNodeOption.Empty) return ConnectionStatus.EmptyOption;
                if (this.DataType != typeof(object)) return base.CanConnect(op);
                if (this.IsInput == op.IsInput) return ConnectionStatus.SameInputOrOutput;
                if (op.Owner == null || this.Owner == null) return ConnectionStatus.NoOwner;
                if (op.Owner == this.Owner) return ConnectionStatus.SameOwner;
                if (this.Owner.LockOption || op.Owner.LockOption) return ConnectionStatus.Locked;
                if (this.IsSingle && m_hs_connected.Count == 1) return ConnectionStatus.SingleOption;
                if (op.IsInput && STNodeEditor.CanFindNodePath(op.Owner, this.Owner)) return ConnectionStatus.Loop;
                if (m_hs_connected.Contains(op)) return ConnectionStatus.Exists;
                if (op.DataType == typeof(object)) return ConnectionStatus.ErrorType;

                if (!this.IsInput) return ConnectionStatus.Connected;
                foreach (STNodeOption owner_input in this.Owner.InputOptions)
                {
                    foreach (STNodeOption o in owner_input.ConnectedOption)
                    {
                        if (o == op) return ConnectionStatus.Exists;
                    }
                }
                return ConnectionStatus.Connected;
            }
        }
    }
}
