using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;

using Aga.Controls.Tree.NodeControls;

namespace Aga.Controls.Tree
{
	public partial class TreeViewAdv
	{
		private Cursor _innerCursor = null;
		private IHeaderLayout _headerLayout;

		public override Cursor Cursor
		{
			get
			{
                if (_innerCursor != null)
                    return _innerCursor;
                else
					return base.Cursor;
			}
			set
			{
				base.Cursor = value;
			}
		}

		#region Internal Properties

		private IRowLayout _rowLayout;

		private bool DragMode
		{
			get { return _dragMode; }
			set
			{
				_dragMode = value;
				if (!value)
				{
					StopDragTimer();
					if (_dragBitmap != null)
						_dragBitmap.Dispose();
					_dragBitmap = null;
				}
				else
					StartDragTimer();
			}
		}
		private bool _dragMode;
		
		[DefaultValue(20), Category("Appearance")]
		public int ColumnHeaderHeight
		{
			get
			{
				if (UseColumns)
					return _headerLayout.PreferredHeaderHeight;
				return 0;
			}
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value");
				_headerLayout.PreferredHeaderHeight = value;
				FullUpdate();
			}
		}

		/// <summary>
		/// returns all nodes, which parent is expanded
		/// </summary>
		private IEnumerable<TreeNodeAdv> VisibleNodes
		{
			get
			{
				TreeNodeAdv node = Root;
				while (node != null)
				{
					node = node.NextVisibleNode;
					if (node != null)
						yield return node;
				}
			}
		}

		internal bool SuspendSelectionEvent
		{
			get { return _suspendSelectionEvent; }
			set
			{
				if (value != _suspendSelectionEvent)
				{
					_suspendSelectionEvent = value;
					if (!_suspendSelectionEvent && _fireSelectionEvent)
						OnSelectionChanged();
				}
			}
		}
		private bool _suspendSelectionEvent;
		
		internal List<TreeNodeAdv> RowMap
		{
			get { return _rowMap; }
		}
		private List<TreeNodeAdv> _rowMap;
		
		internal TreeNodeAdv SelectionStart
		{
			get { return _selectionStart; }
			set { _selectionStart = value; }
		}
		private TreeNodeAdv _selectionStart;
		
		internal InputState Input
		{
			get { return _input; }
			set
			{
				_input = value;
			}
		}
		private InputState _input;
		
		internal bool ItemDragMode
		{
			get { return _itemDragMode; }
			set { _itemDragMode = value; }
		}
		private bool _itemDragMode;
		
		internal Point ItemDragStart
		{
			get { return _itemDragStart; }
			set { _itemDragStart = value; }
		}
		private Point _itemDragStart;
		
		/// <summary>
		/// Number of rows fits to the current page
		/// </summary>
		internal int CurrentPageSize
		{
			get
			{
				return _rowLayout.CurrentPageSize;
			}
		}
		
		/// <summary>
		/// Number of all visible nodes (which parent is expanded)
		/// </summary>
		internal int RowCount
		{
			get
			{
				return RowMap.Count;
			}
		}
		
		/// <summary>
		/// Number of all visible, non-hidden nodes (which parent is expanded)
		/// </summary>
		internal int VisibleRowCount
		{
			get
			{
				int visibleCount = 0;
				for (int i = 0; i < this.RowMap.Count; i++)
				{
					if (this.RowMap[i].IsHidden)
						continue;

					visibleCount++;
				}
				return visibleCount;
			}
		}
		
		private int ContentWidth
		{
			get
			{
				return _contentWidth;
			}
		}
		private int _contentWidth = 0;
		
		internal int FirstVisibleRow
		{
			get { return _firstVisibleRow; }
			set
			{
				HideEditor();
				_firstVisibleRow = value;
				UpdateView();
			}
		}
		private int _firstVisibleRow;
		
		public int OffsetX
		{
			get { return _offsetX; }
			private set
			{
				HideEditor();
				_offsetX = value;
				UpdateView();
			}
		}
		private int _offsetX;
		
		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle r = ClientRectangle;
				//r.Y += ColumnHeaderHeight;
				//r.Height -= ColumnHeaderHeight;
				int w = _vScrollBar.Visible ? _vScrollBar.Width : 0;
				int h = _hScrollBar.Visible ? _hScrollBar.Height : 0;
				return new Rectangle(r.X, r.Y, r.Width - w, r.Height - h);
			}
		}
		
		internal List<TreeNodeAdv> Selection
		{
			get { return _selection; }
		}
		private List<TreeNodeAdv> _selection;
		
		#endregion

		#region Public Properties

		#region DesignTime
		
		[DefaultValue(typeof(Color), "Highlight"), Category("Appearance")]
		public Color FullRowSelectActiveColor
		{
			get { return _fullRowSelectActiveColor; }
			set { _fullRowSelectActiveColor = value; }
		}
		private Color _fullRowSelectActiveColor = SystemColors.Highlight;
		
		[DefaultValue(typeof(Color), "Control"), Category("Appearance")]
		public Color FullRowSelectInactiveColor
		{
			get { return _fullRowSelectInactiveColor; }
			set { _fullRowSelectInactiveColor = value; }
		}
		private Color _fullRowSelectInactiveColor = SystemColors.Control;
		
		[DefaultValue(false), Category("Behavior")]
		public bool ShiftFirstNode
		{
			get { return _shiftFirstNode; }
			set { _shiftFirstNode = value; }
		}
		private bool _shiftFirstNode;
		
		[DefaultValue(false), Category("Behavior")]
		public bool DisplayDraggingNodes
		{
			get { return _displayDraggingNodes; }
			set { _displayDraggingNodes = value; }
		}
		private bool _displayDraggingNodes;
		
		[DefaultValue(false), Category("Behavior")]
		public bool FullRowSelect
		{
			get { return _fullRowSelect; }
			set
			{
				_fullRowSelect = value;
				UpdateView();
			}
		}
		private bool _fullRowSelect;
		
		[DefaultValue(false), Category("Behavior")]
		public bool UseColumns
		{
			get { return _useColumns; }
			set
			{
				_useColumns = value;
				FullUpdate();
			}
		}
		private bool _useColumns;
		
		[DefaultValue(false), Category("Behavior")]
		public bool AllowColumnReorder
		{
			get { return _allowColumnReorder; }
			set { _allowColumnReorder = value; }
		}
		private bool _allowColumnReorder;
		
		[DefaultValue(true), Category("Behavior")]
		public bool ShowLines
		{
			get { return _showLines; }
			set
			{
				_showLines = value;
				UpdateView();
			}
		}
		private bool _showLines = true;
		
		[DefaultValue(true), Category("Behavior")]
		public bool ShowPlusMinus
		{
			get { return _showPlusMinus; }
			set
			{
				_showPlusMinus = value;
				FullUpdate();
			}
		}
		private bool _showPlusMinus = true;
		
		[DefaultValue(false), Category("Behavior")]
		public bool ShowNodeToolTips
		{
			get { return _showNodeToolTips; }
			set { _showNodeToolTips = value; }
		}
		private bool _showNodeToolTips = false;
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), DefaultValue(true), Category("Behavior"), Obsolete("No longer used")]
		public bool KeepNodesExpanded
		{
			get { return true; }
			set {}
		}
		
        /// <Summary>
        /// The model associated with this <see cref="TreeViewAdv"/>.
        /// </Summary>
        /// <seealso cref="ITreeModel"/>
        /// <seealso cref="TreeModel"/>
        [Browsable(false)]
		public ITreeModel Model
		{
			get { return _model; }
			set
			{
				if (_model != value)
				{
					AbortBackgroundExpandingThreads();
					if (_model != null)
						UnbindModelEvents();
					_model = value;
					CreateNodes();
					FullUpdate();
					if (_model != null)
						BindModelEvents();
				}
			}
		}
		private ITreeModel _model;
		
        // Tahoma is the default font
        private static Font _font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)), false);
        /// <summary>
        /// The font to render <see cref="TreeViewAdv"/> content in.
        /// </summary>
        [Category("Appearance"), Description("The font to render TreeViewAdv content in.")]
        public override Font Font
        {
            get
            {
                return (base.Font);
            }
            set
            {
                if (value == null)
                    base.Font = _font;
                else
                {
                    if (value == DefaultFont)
                        base.Font = _font;
                    else
                        base.Font = value;
                }
            }
        }
        public override void ResetFont()
        {
            Font = null;
        }
        private bool ShouldSerializeFont()
        {
            return (!Font.Equals(_font));
        }
        // End font property
		
		[DefaultValue(BorderStyle.Fixed3D), Category("Appearance")]
		public BorderStyle BorderStyle
		{
			get
			{
				return this._borderStyle;
			}
			set
			{
				if (_borderStyle != value)
				{
					_borderStyle = value;
					base.UpdateStyles();
				}
			}
		}
		private BorderStyle _borderStyle = BorderStyle.Fixed3D;
		
		[DefaultValue(typeof(Color), "Window"), Category("Appearance")]
		public override Color BackColor
		{
			get { return base.BackColor; }
			set { base.BackColor = value; }
		}
		
		/// <summary>
		/// Set to true to expand each row's height to fit the text of it's largest column.
		/// </summary>
		[DefaultValue(false), Category("Appearance"), Description("Expand each row's height to fit the text of it's largest column.")]
		public bool AutoRowHeight
		{
			get
			{
				return _autoRowHeight;
			}
			set
			{
				_autoRowHeight = value;
				if (value)
					_rowLayout = new AutoRowHeightLayout(this, RowHeight);
				else
					_rowLayout = new FixedRowHeightLayout(this, RowHeight);
				FullUpdate();
			}
		}
		private bool _autoRowHeight = false;
		
		/// <summary>
		/// Set to true to expand header height to fit the text of it's largest column.
		/// </summary>
		[DefaultValue(false), Category("Appearance"), Description("Expand each header height to fit the text of it's largest column.")]
		public bool AutoHeaderHeight
		{
			get
			{
				return _autoHeaderHeight;
			}
			set
			{
				_autoHeaderHeight = value;
				if (value)
					_headerLayout = new AutoHeaderHeightLayout(this, ColumnHeaderHeight);
				else
					_headerLayout = new FixedHeaderHeightLayout(this, ColumnHeaderHeight);
				FullUpdate();
			}
		}
		private bool _autoHeaderHeight = false;
		
        [DefaultValue(GridLineStyle.None), Category("Appearance")]
        public GridLineStyle GridLineStyle
        {
            get
            {
                return _gridLineStyle;
            }
            set
            {
				if (value != _gridLineStyle)
				{
					_gridLineStyle = value;
					UpdateView();
					OnGridLineStyleChanged();
				}
            }
        }
        private GridLineStyle _gridLineStyle = GridLineStyle.None;
        
		[DefaultValue(16), Category("Appearance")]
		public int RowHeight
		{
			get
			{
				return _rowHeight;
			}
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException("value");

				_rowHeight = value;
				_rowLayout.PreferredRowHeight = value;
				FullUpdate();
			}
		}
		private int _rowHeight = 16;
		
		[DefaultValue(TreeSelectionMode.Single), Category("Behavior")]
		public TreeSelectionMode SelectionMode
		{
			get { return _selectionMode; }
			set { _selectionMode = value; }
		}
		private TreeSelectionMode _selectionMode = TreeSelectionMode.Single;
		
		[DefaultValue(false), Category("Behavior")]
		public bool HideSelection
		{
			get { return _hideSelection; }
			set
			{
				_hideSelection = value;
				UpdateView();
			}
		}
		private bool _hideSelection;
		
		[DefaultValue(true), Category("Behavior")]
		public bool InactiveSelection
		{
			get { return _inactiveSelection; }
			set
			{
				_inactiveSelection = value;
				UpdateView();
			}
		}
		bool _inactiveSelection = true;
		
		[DefaultValue(0.3f), Category("Behavior")]
		public float TopEdgeSensivity
		{
			get { return _topEdgeSensivity; }
			set
			{
				if (value < 0 || value > 1)
					throw new ArgumentOutOfRangeException();
				_topEdgeSensivity = value;
			}
		}
		private float _topEdgeSensivity = 0.3f;
		
		[DefaultValue(0.3f), Category("Behavior")]
		public float BottomEdgeSensivity
		{
			get { return _bottomEdgeSensivity; }
			set
			{
				if (value < 0 || value > 1)
					throw new ArgumentOutOfRangeException("value should be from 0 to 1");
				_bottomEdgeSensivity = value;
			}
		}
		private float _bottomEdgeSensivity = 0.3f;
		
		[DefaultValue(false), Category("Behavior")]
		public bool LoadOnDemand
		{
			get { return _loadOnDemand; }
			set { _loadOnDemand = value; }
		}
		private bool _loadOnDemand;
		
		[DefaultValue(false), Category("Behavior")]
		public bool UnloadCollapsedOnReload
		{
			get { return _unloadCollapsedOnReload; }
			set { _unloadCollapsedOnReload = value; }
		}
		private bool _unloadCollapsedOnReload = false;
		
		[DefaultValue(19), Category("Behavior")]
		public int Indent
		{
			get { return _indent; }
			set
			{
				_indent = value;
				UpdateView();
			}
		}
		private int _indent = 19;
		
		[DefaultValue(typeof(Color), "ControlDark"), Category("Behavior")]
		public Color LineColor
		{
			get { return _lineColor; }
			set
			{
				_lineColor = value;
				CreateLinePen();
				UpdateView();
			}
		}
		private Color _lineColor = SystemColors.ControlDark;
		
		[DefaultValue(typeof(Color), "Black"), Category("Behavior")]
		public Color DragDropMarkColor
		{
			get { return _dragDropMarkColor; }
			set
			{
				_dragDropMarkColor = value;
				CreateMarkPen();
			}
		}
		private Color _dragDropMarkColor = Color.Black;
		
		[DefaultValue(3.0f), Category("Behavior")]
		public float DragDropMarkWidth
		{
			get { return _dragDropMarkWidth; }
			set
			{
				_dragDropMarkWidth = value;
				CreateMarkPen();
			}
		}
		private float _dragDropMarkWidth = 3.0f;
		
		[DefaultValue(true), Category("Behavior")]
		public bool HighlightDropPosition
		{
			get { return _highlightDropPosition; }
			set { _highlightDropPosition = value; }
		}
		private bool _highlightDropPosition = true;
		
		[Category("Behavior"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public Collection<TreeColumn> Columns
		{
			get { return _columns; }
		}
		private TreeColumnCollection _columns;
		
		[Category("Behavior"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[Editor(typeof(NodeControlCollectionEditor), typeof(UITypeEditor))]
		public Collection<NodeControl> NodeControls
		{
			get
			{
				return _controls;
			}
		}
		private NodeControlsCollection _controls;
		
		/// <summary>
		/// When set to true, node contents will be read in background thread.
		/// </summary>
		[Category("Behavior"), DefaultValue(false), Description("Read children in a background thread when expanding.")]
		public bool AsyncExpanding
		{
			get { return _asyncExpanding; }
			set { _asyncExpanding = value; }
		}
		private bool _asyncExpanding;
		
		#endregion

		#region RunTime
		
		[Browsable(false)]
		public IToolTipProvider DefaultToolTipProvider
		{
			get { return _defaultToolTipProvider; }
			set { _defaultToolTipProvider = value; }
		}
		private IToolTipProvider _defaultToolTipProvider = null;
		
		[Browsable(false)]
		public IEnumerable<TreeNodeAdv> AllNodes
		{
			get
			{
				if (_root.Nodes.Count > 0)
				{
					TreeNodeAdv node = _root.Nodes[0];
					while (node != null)
					{
						yield return node;
						if (node.Nodes.Count > 0)
							node = node.Nodes[0];
						else if (node.NextNode != null)
							node = node.NextNode;
						else
							node = node.BottomNode;
					}
				}
			}
		}
		
		[Browsable(false)]
		public Predicate<TreeNodeAdv> NodeFilter
		{
			get { return this._viewNodeFilter; }
			set
			{
				this._viewNodeFilter = value;
				this.UpdateNodeFilter();
			}
		}
		
		[Browsable(false)]
		public DropPosition DropPosition
		{
			get { return _dropPosition; }
			set { _dropPosition = value; }
		}
		private DropPosition _dropPosition;
		
		[Browsable(false)]
		public TreeNodeAdv Root
		{
			get { return _root; }
		}
		private TreeNodeAdv _root;
		
		[Browsable(false)]
		public ReadOnlyCollection<TreeNodeAdv> SelectedNodes
		{
			get
			{
				return _readonlySelection;
			}
		}
		private ReadOnlyCollection<TreeNodeAdv> _readonlySelection;
		
		[Browsable(false)]
		public TreeNodeAdv SelectedNode
		{
			get
			{
				if (Selection.Count > 0)
				{
					if (CurrentNode != null && CurrentNode.IsSelected)
						return CurrentNode;
					else
						return Selection[0];
				}
				else
					return null;
			}
			set
			{
				if (SelectedNode == value)
					return;

				BeginUpdate();
				try
				{
					if (value == null)
					{
						ClearSelectionInternal();
					}
					else
					{
						if (!IsMyNode(value))
							throw new ArgumentException();

						ClearSelectionInternal();
						value.IsSelected = true;
						CurrentNode = value;
						EnsureVisible(value);
					}
				}
				finally
				{
					EndUpdate();
				}
			}
		}
		
		[Browsable(false)]
		public TreeNodeAdv CurrentNode
		{
			get { return _currentNode; }
			internal set { _currentNode = value; }
		}
		private TreeNodeAdv _currentNode;
		
        [Browsable(false)]
        public int ItemCount
        {
            get { return RowMap.Count; }
        }
		
		/// <summary>
		/// Indicates the distance the content is scrolled to the left
		/// </summary>
		[Browsable(false)]
		public int HorizontalScrollPosition
		{
			get
			{
				if (_hScrollBar.Visible)
					return _hScrollBar.Value;
				else
					return 0;
			}
		}

		#endregion

		#endregion

	}
}
