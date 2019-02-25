﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using SmartCmdArgs.Helper;
using SmartCmdArgs.ViewModel;

namespace SmartCmdArgs.View
{
    public class TreeViewEx : TreeView
    {
        static TreeViewEx()
        {
            RegisterCommand(ApplicationCommands.Copy, CopyCommandProperty);
            RegisterCommand(ApplicationCommands.Paste, PasteCommandProperty);
            RegisterCommand(ApplicationCommands.Cut, CutCommandProperty);
            RegisterCommand(ApplicationCommands.Delete, DeleteCommandProperty);

            CommandManager.RegisterClassCommandBinding(typeof(TreeViewEx), new CommandBinding(ApplicationCommands.SelectAll, 
                (sender, args) => ((TreeViewEx)sender).SelectAllItems(args), (sender, args) => args.CanExecute = ((TreeViewEx)sender).HasItems));

            void RegisterCommand(RoutedUICommand routedUiCommand, DependencyProperty commandProperty)
            {
                CommandManager.RegisterClassCommandBinding(typeof(TreeViewEx), 
                    new CommandBinding(
                        routedUiCommand, 
                        (sender, args) => ((ICommand)((DependencyObject)sender).GetValue(commandProperty))?.Execute(args.Parameter), 
                        (sender, args) => args.CanExecute = ((ICommand)((DependencyObject)sender).GetValue(commandProperty)).CanExecute(args.Parameter)));
            }
        }

        public static readonly DependencyProperty CopyCommandProperty = DependencyProperty.Register(
            nameof(CopyCommand), typeof(ICommand), typeof(TreeViewEx), new PropertyMetadata(default(ICommand)));
        public static readonly DependencyProperty PasteCommandProperty = DependencyProperty.Register(
            nameof(PasteCommand), typeof(ICommand), typeof(TreeViewEx), new PropertyMetadata(default(ICommand)));
        public static readonly DependencyProperty CutCommandProperty = DependencyProperty.Register(
            nameof(CutCommand), typeof(ICommand), typeof(TreeViewEx), new PropertyMetadata(default(ICommand)));
        public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register(
            nameof(DeleteCommand), typeof(ICommand), typeof(TreeViewEx), new PropertyMetadata(default(ICommand)));
        public ICommand CopyCommand { get => (ICommand)GetValue(CopyCommandProperty); set => SetValue(CopyCommandProperty, value); }
        public ICommand PasteCommand { get => (ICommand)GetValue(PasteCommandProperty); set => SetValue(PasteCommandProperty, value); }
        public ICommand CutCommand { get => (ICommand)GetValue(CutCommandProperty); set => SetValue(CutCommandProperty, value); }
        public ICommand DeleteCommand { get => (ICommand)GetValue(DeleteCommandProperty); set => SetValue(DeleteCommandProperty, value); }

        public static readonly DependencyProperty ToggleSelectedCommandProperty = DependencyProperty.Register(
            nameof(ToggleSelectedCommand), typeof(ICommand), typeof(TreeViewEx), new PropertyMetadata(default(ICommand)));
        public ICommand ToggleSelectedCommand { get => (ICommand)GetValue(ToggleSelectedCommandProperty); set => SetValue(ToggleSelectedCommandProperty, value); }

        public static readonly DependencyProperty SelectIndexCommandProperty = DependencyProperty.Register(
            nameof(SelectIndexCommand), typeof(ICommand), typeof(TreeViewEx), new PropertyMetadata(default(ICommand)));
        public ICommand SelectIndexCommand { get => (ICommand)GetValue(SelectIndexCommandProperty); set => SetValue(SelectIndexCommandProperty, value); }

        public static readonly DependencyProperty SelectItemCommandProperty = DependencyProperty.Register(
            nameof(SelectItemCommand), typeof(ICommand), typeof(TreeViewEx), new PropertyMetadata(default(ICommand)));
        public ICommand SelectItemCommand { get => (ICommand)GetValue(SelectItemCommandProperty); set => SetValue(SelectItemCommandProperty, value); }
        

        public static readonly DependencyProperty SplitArgumentCommandProperty = DependencyProperty.Register(
            nameof(SplitArgumentCommand), typeof(ICommand), typeof(TreeViewEx), 
            new PropertyMetadata(default(ICommand), (d, e) => ((TreeViewEx)d)._splitArgumentMenuItem.Command = (ICommand)e.NewValue));
        public ICommand SplitArgumentCommand { get { return (ICommand)GetValue(SplitArgumentCommandProperty); } set { SetValue(SplitArgumentCommandProperty, value); } }
        
        public static readonly DependencyProperty NewGroupFromArgumentsCommandProperty = DependencyProperty.Register(
            nameof(NewGroupFromArgumentsCommand), typeof(ICommand), typeof(TreeViewEx), 
            new PropertyMetadata(default(ICommand), (d, e) => ((TreeViewEx)d)._newGroupFromArgumentsMenuItem.Command = (ICommand)e.NewValue));
        public ICommand NewGroupFromArgumentsCommand { get { return (ICommand)GetValue(NewGroupFromArgumentsCommandProperty); } set { SetValue(NewGroupFromArgumentsCommandProperty, value); } }
        
        public static readonly DependencyProperty SetAsStartupProjectCommandProperty = DependencyProperty.Register(
            nameof(SetAsStartupProjectCommand), typeof(ICommand), typeof(TreeViewEx), 
            new PropertyMetadata(default(ICommand), (d, e) => ((TreeViewEx)d)._setAsStartupProjectMenuItem.Command = (ICommand)e.NewValue));
        public ICommand SetAsStartupProjectCommand { get { return (ICommand)GetValue(SetAsStartupProjectCommandProperty); } set { SetValue(SetAsStartupProjectCommandProperty, value); } }

        public static readonly DependencyProperty SetProjectConfigCommandProperty = DependencyProperty.Register(
            nameof(SetProjectConfigCommand), typeof(ICommand), typeof(TreeViewEx), new PropertyMetadata(default(ICommand)));
        public ICommand SetProjectConfigCommand { get { return (ICommand)GetValue(SetProjectConfigCommandProperty); } set { SetValue(SetProjectConfigCommandProperty, value); } }


        protected override DependencyObject GetContainerForItemOverride() => new TreeViewItemEx(this);
        protected override bool IsItemItsOwnContainerOverride(object item) => item is TreeViewItemEx;

        // taken from https://stackoverflow.com/questions/459375/customizing-the-treeview-to-allow-multi-select

        // Used in shift selections
        private TreeViewItemEx _lastItemSelected;

        public static readonly DependencyProperty IsItemSelectedProperty =
            DependencyProperty.RegisterAttached("IsItemSelected", typeof(bool), typeof(TreeViewEx));

        public static void SetIsItemSelected(UIElement element, bool value)
        {
            element.SetValue(IsItemSelectedProperty, value);
        }
        public static bool GetIsItemSelected(UIElement element)
        {
            return (bool)element.GetValue(IsItemSelectedProperty);
        }
        
        private static bool IsCtrlPressed => (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        private static bool IsShiftPressed => (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;


        public IEnumerable<TreeViewItemEx> SelectedTreeViewItems => GetTreeViewItems(this, true).Where(GetIsItemSelected);

        public IEnumerable<TreeViewItemEx> VisibleTreeViewItems => GetTreeViewItems(this, false);

        private MenuItem _splitArgumentMenuItem;
        private MenuItem _newGroupFromArgumentsMenuItem;
        private MenuItem _setAsStartupProjectMenuItem;
        private MenuItem _projConfigMenuItem;

        public TreeViewEx()
        {
            // TODO: Implement ContextMenu
            ContextMenu = new ContextMenu();
            ContextMenu.Items.Add(new MenuItem { Command = ApplicationCommands.Cut });
            ContextMenu.Items.Add(new MenuItem { Command = ApplicationCommands.Copy });
            ContextMenu.Items.Add(new MenuItem { Command = ApplicationCommands.Paste });
            ContextMenu.Items.Add(new MenuItem { Command = ApplicationCommands.Delete });
            ContextMenu.Items.Add(new Separator());
            ContextMenu.Items.Add(_newGroupFromArgumentsMenuItem = new MenuItem { Header = "New Group from Selection" });
            ContextMenu.Items.Add(_splitArgumentMenuItem = new MenuItem { Header = "Split Argument" });
            ContextMenu.Items.Add(_setAsStartupProjectMenuItem = new MenuItem { Header = "Set as sigle Startup Project" });
            ContextMenu.Items.Add(_projConfigMenuItem = new MenuItem { Header = "Project Config" });

            CollapseWhenDisbaled(_splitArgumentMenuItem);
            CollapseWhenDisbaled(_setAsStartupProjectMenuItem);
            CollapseWhenDisbaled(_projConfigMenuItem);

            DataContextChanged += OnDataContextChanged;
            ContextMenuOpening += OnContextMenuOpening;
        }

        private void OnDataContextChanged(object tv, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            SelectIndexCommand = new RelayCommand<int>(idx =>
            {
                var shouldFocus = TreeHelper.FindAncestorOrSelf<Border>(this, "PART_ContentPanel")?.IsKeyboardFocusWithin ?? false;

                var curIdx = 0;
                TreeViewItemEx focusItem = null;
                foreach (var treeViewItem in GetTreeViewItems(this, false))
                {
                    SetIsItemSelected(treeViewItem, false);
                    if (idx == curIdx)
                    {
                        SetIsItemSelected(treeViewItem, true);
                        _lastItemSelected = treeViewItem;
                        if (shouldFocus)
                            focusItem = treeViewItem;
                    }
                    curIdx++;
                }
                focusItem?.Focus();
            }, i => i >= 0);

            SelectItemCommand = new RelayCommand<object>(item =>
            {
                TreeViewItemEx focusItem = null;
                foreach (var treeViewItem in GetTreeViewItems(this, false))
                {
                    SetIsItemSelected(treeViewItem, false);
                    if (item == treeViewItem.Item)
                    {
                        SetIsItemSelected(treeViewItem, true);
                        _lastItemSelected = treeViewItem;
                        focusItem = treeViewItem;
                    }
                }
                focusItem?.Focus();
            }, o => o != null);
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _projConfigMenuItem.Items.Clear();
            var item = SelectedTreeViewItems.FirstOrDefault()?.Item as CmdGroup;
            if (item != null)
            {
                CmdContainer con = item.Parent;
                _projConfigMenuItem.IsEnabled = SetProjectConfigCommand.CanExecute(null);
                if (_projConfigMenuItem.IsEnabled)
                {
                    _projConfigMenuItem.Items.Add(new MenuItem
                    {
                        Header = "All",
                        Command = SetProjectConfigCommand,
                        CommandParameter = null,
                        IsChecked = item.ProjectConfig == null,
                        IsCheckable = true
                    });

                    while (!(con is CmdProject))
                        con = con.Parent;

                    var proj = (CmdProject)con;
                    foreach (var config in proj.Configurations)
                    {
                        _projConfigMenuItem.Items.Add(new MenuItem {
                            Header = config,
                            Command = SetProjectConfigCommand,
                            CommandParameter = config,
                            IsChecked = item.ProjectConfig == config,
                            IsCheckable = true
                        });
                    }
                }
            }
            else
                _projConfigMenuItem.IsEnabled = false;
        }

        private void CollapseWhenDisbaled(FrameworkElement element)
        {
            element.SetBinding(FrameworkElement.VisibilityProperty, new Binding
            {
                Source = element,
                Path = new PropertyPath(nameof(FrameworkElement.IsEnabled)),
                Mode = BindingMode.OneWay,
                Converter = new BooleanToVisibilityConverter()
            });
        }

        public void ChangedFocusedItem(TreeViewItemEx item)
        {
            if (Keyboard.IsKeyDown(Key.Up)
                || Keyboard.IsKeyDown(Key.Down)
                || Keyboard.IsKeyDown(Key.Left)
                || Keyboard.IsKeyDown(Key.Right)
                || Keyboard.IsKeyDown(Key.Prior)
                || Keyboard.IsKeyDown(Key.Next)
                || Keyboard.IsKeyDown(Key.End)
                || Keyboard.IsKeyDown(Key.Home))
            {
                SelectedItemChangedInternal(item);
            }

            if (!GetIsItemSelected(item))
            {
                var aSelectedItem = SelectedTreeViewItems.FirstOrDefault();
                if (aSelectedItem != null)
                {
                    _lastItemSelected = aSelectedItem;
                    aSelectedItem.Focus();
                }
                else
                {
                    SetIsItemSelected(item, true);
                    _lastItemSelected = item;
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            foreach (var treeViewItem in GetTreeViewItems(this, false))
            {
                var cmdItem = treeViewItem.Item;
                if (cmdItem.IsInEditMode)
                {
                    cmdItem.CommitEdit();
                    e.Handled = true;
                }
            }
        }
        
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = ToggleSelectedCommand?.SafeExecute() == true;
            }
            base.OnKeyDown(e);
        }

        private void SelectedItemChangedInternal(TreeViewItemEx tvItem)
        {
            // Clear all previous selected item states if ctrl is NOT being held down
            if (!IsCtrlPressed)
            {
                foreach (var treeViewItem in GetTreeViewItems(this, true))
                    SetIsItemSelected(treeViewItem, false);
            }
            
            // Is this an item range selection?
            if (IsShiftPressed && _lastItemSelected != null)
            {
                var items = GetTreeViewItemRange(_lastItemSelected, tvItem);
                if (items.Count > 0)
                {
                    foreach (var treeViewItem in items)
                        SetIsItemSelected(treeViewItem, true);

                    //_lastItemSelected = items.Last();
                }
            }
            // Otherwise, individual selection (toggle if CTRL is Pressed)
            else
            {
                SetIsItemSelected(tvItem, !IsCtrlPressed || !GetIsItemSelected(tvItem));
                _lastItemSelected = tvItem;
            }
        }
        private static IEnumerable<TreeViewItemEx> GetTreeViewItems(ItemsControl parentItem, bool includeCollapsedItems)
        {
            for (var index = 0; index < parentItem.Items.Count; index++)
            {
                var tvItem = parentItem.ItemContainerGenerator.ContainerFromIndex(index) as TreeViewItemEx;
                if (tvItem == null) continue;

                yield return tvItem;
                if (includeCollapsedItems || tvItem.IsExpanded)
                {
                    foreach (var item in GetTreeViewItems(tvItem, includeCollapsedItems))
                        yield return item;
                }
            }
        }
        private List<TreeViewItemEx> GetTreeViewItemRange(TreeViewItemEx start, TreeViewItemEx end)
        {
            var items = GetTreeViewItems(this, false).ToList();

            var startIndex = items.IndexOf(start);
            var endIndex = items.IndexOf(end);
            var rangeStart = startIndex > endIndex || startIndex == -1 ? endIndex : startIndex;
            var rangeCount = startIndex > endIndex ? startIndex - endIndex + 1 : endIndex - startIndex + 1;

            if (startIndex == -1 && endIndex == -1)
                rangeCount = 0;

            else if (startIndex == -1 || endIndex == -1)
                rangeCount = 1;

            return rangeCount > 0 ? items.GetRange(rangeStart, rangeCount) : new List<TreeViewItemEx>();
        }

        public void SelectItem(TreeViewItemEx item)
        {
            SetIsItemSelected(item, true);
            _lastItemSelected = item;
        }

        public void DeselectItem(TreeViewItemEx item)
        {
            SetIsItemSelected(item, false);
            SelectedTreeViewItems.FirstOrDefault()?.Focus();
        }

        public void SelectItemExclusively(TreeViewItemEx item)
        {
            var items = GetTreeViewItems(this, includeCollapsedItems: true);
            foreach (var treeViewItem in items)
            {
                if (treeViewItem == item)
                {
                    if (!GetIsItemSelected(item))
                    {
                        SetIsItemSelected(treeViewItem, true);
                        _lastItemSelected = treeViewItem;
                    }
                }
                else
                {
                    if (treeViewItem.Item.IsInEditMode)
                    {
                        treeViewItem.Item.CommitEdit();
                    }
                    
                    SetIsItemSelected(treeViewItem, false);
                }
            }
        }

        public void RangeSelect(TreeViewItemEx tvItem)
        {
            foreach (var treeViewItem in GetTreeViewItems(this, true))
                SetIsItemSelected(treeViewItem, false);

            if (_lastItemSelected != null)
            {
                var items = GetTreeViewItemRange(_lastItemSelected, tvItem);
                if (items.Count > 0)
                {
                    foreach (var treeViewItem in items)
                        SetIsItemSelected(treeViewItem, true);
                }
            }
            else
            {
                SelectItem(tvItem);
            }
        }

        private void SelectAllItems(ExecutedRoutedEventArgs args)
        {
            foreach (var treeViewItem in GetTreeViewItems(this, false))
            {
                SetIsItemSelected(treeViewItem, true);
            }
            args.Handled = true;
        }

        public void ClearSelection()
        {
            var items = GetTreeViewItems(this, includeCollapsedItems: true);
            foreach (var treeViewItem in items)
            {
                SetIsItemSelected(treeViewItem, false);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            DragDrop.OnMouseMove(this, e);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (HasItems)
            {
                var item = (TreeViewItemEx)ItemContainerGenerator.ContainerFromIndex(Items.Count - 1);
                DragDrop.OnDragEnter(item, e);
            }
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            if (HasItems)
                DragDrop.OnDragOver((TreeViewItemEx)ItemContainerGenerator.ContainerFromIndex(Items.Count - 1), e);
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            if (HasItems)
                DragDrop.OnDragLeave((TreeViewItemEx)ItemContainerGenerator.ContainerFromIndex(Items.Count - 1), e);
        }

        protected override void OnDrop(DragEventArgs e)
        {
            if (HasItems)
                DragDrop.HandleDropForTarget((TreeViewItemEx)ItemContainerGenerator.ContainerFromIndex(Items.Count - 1), e);
        }
    }

    public class TreeViewItemEx : TreeViewItem
    {
        // Mouse state variables
        private bool justReceivedSelection = false;
        private CancellationTokenSource leftSingleClickCancelSource = null;
        private int leftMouseButtonClickCount = 0;

        public FrameworkElement ItemBorder => GetTemplateChild("ItemBorder") as FrameworkElement;
        public FrameworkElement HeaderBorder => GetTemplateChild("HeaderBorder") as FrameworkElement;

        public CmdBase Item => DataContext as CmdBase;

        private static bool IsCtrlPressed => (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        private static bool IsShiftPressed => (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        public TreeViewEx ParentTreeView { get; }

        public int Level
        {
            get { return (int)GetValue(LevelProperty); }
            set { SetValue(LevelProperty, value); }
        }

        protected override DependencyObject GetContainerForItemOverride() => new TreeViewItemEx(ParentTreeView, this.Level+1);
        protected override bool IsItemItsOwnContainerOverride(object item) => item is TreeViewItemEx;
        
        public event KeyEventHandler HandledKeyDown
        {
            add => AddHandler(KeyDownEvent, value, true);
            remove => RemoveHandler(KeyDownEvent, value);
        }

        public TreeViewItemEx(TreeViewEx parentTreeView, int level = 0)
        {
            ParentTreeView = parentTreeView;
            Level = level;

            DataContextChanged += OnDataContextChanged;
            HandledKeyDown += OnHandledKeyDown;
            RequestBringIntoView += OnRequestBringIntoView;
        }

        private void OnHandledKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !Item.IsInEditMode && Item.IsSelected)
            {
                var items = ParentTreeView.VisibleTreeViewItems.ToList();
                var indexToSelect = items.IndexOf(this);
                if (indexToSelect >= 0)
                {
                    indexToSelect = Math.Min(items.Count - 1, indexToSelect + 1);
                    ParentTreeView.SelectIndexCommand.SafeExecute(indexToSelect);
                }
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            BindingOperations.ClearBinding(this, TreeViewEx.IsItemSelectedProperty);
            BindingOperations.ClearBinding(this, IsExpandedProperty);

            if (e.OldValue is CmdBase oldCmd)
            {
                oldCmd.IsFocusedItem = false;
            }

            if (e.NewValue is CmdBase newCmd)
            {
                newCmd.IsFocusedItem = IsKeyboardFocusWithin;

                SetBinding(TreeViewEx.IsItemSelectedProperty, new Binding
                {
                    Source = e.NewValue,
                    Path = new PropertyPath(nameof(CmdBase.IsSelected)),
                    Mode = BindingMode.TwoWay
                });
            }

            if (e.NewValue is CmdContainer)
            {
                SetBinding(IsExpandedProperty, new Binding
                {
                    Source = e.NewValue,
                    Path = new PropertyPath(nameof(CmdContainer.IsExpanded)),
                    Mode = BindingMode.TwoWay
                });
            }
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            if (IsFocused 
                && Item.IsEditable 
                && !Item.IsInEditMode 
                && !string.IsNullOrEmpty(e.Text)
                && !char.IsControl(e.Text[0]))
            {
                 Item.BeginEdit(initialValue: e.Text);
            }

            base.OnTextInput(e);
        }


        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (IsFocused)
            {
                if (e.Key == Key.Return || e.Key == Key.F2)
                {
                    if (Item.IsEditable && !Item.IsInEditMode)
                    {
                        Item.BeginEdit();
                        e.Handled = true;
                    }
                }
            }
        }

        private void EnterEditModeDelayed()
        {
            Debug.WriteLine("Triggered delayed enter edit mode");

            // Wait for possible double click.
            // Single click => edit; double click => toggle expand state
            leftSingleClickCancelSource?.Cancel();

            if (!Item.IsEditable)
                return;

            leftSingleClickCancelSource = new CancellationTokenSource();

            var doubleClickTime = TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime);
            DelayExecution.ExecuteAfter(doubleClickTime, leftSingleClickCancelSource.Token, () =>
            {
                Debug.WriteLine("Delayed edit mode");
                // Focus might have changed since first click
                if (IsFocused)
                {
                    Item.BeginEdit();
                }
            });
        }

        private void HandleMouseClick(MouseButtonEventArgs e, bool down, bool isShiftPressed, bool isCtrlPressed)
        {
            // Button    Key    Selection    Click Target    Event    Action
            // =======================================================================
            // Left      None   Single       Unselected      Down     SelectExclusive
            //                               Selected        Up       DelayedEditMode
            //                  Multi        Unselected      Down     SelectExclusive
            //                               Selected        Up       SelectExclusive
            //           Shift  Single       Unselected      Down     RangeSelect
            //                               Selected        Up       DelayedEditMode
            //                  Multi        Unselected      Down     RangeSelect
            //                               Selected        Down     RangeSelect
            //           Ctrl   Single       Unselected      Down     AddToSelection
            //                               Selected        Up       DeselctItem
            //                  Multi        Unselected      Down     AddToSelection
            //                               Selected        Up       DeselctItem
            //
            // Right     None   Single       Unselected      Down     SelectExclusive

            // Right-click allways triggers Context Menu on MouseUp so only down can be handeled here.

            if (e.ChangedButton == MouseButton.Left)
            {
                if (!isShiftPressed && !isCtrlPressed)
                {
                    if (!ParentTreeView.SelectedTreeViewItems.HasMultipleItems())
                    {
                        if (!Item.IsSelected && down)
                            ParentTreeView.SelectItemExclusively(this);
                        else if (Item.IsSelected && !down)
                            EnterEditModeDelayed();
                    }
                    else
                    {
                        if ((!Item.IsSelected && down) || (Item.IsSelected && !down))
                            ParentTreeView.SelectItemExclusively(this);
                    }
                }
                else if (isShiftPressed && !isCtrlPressed)
                {
                    if (!ParentTreeView.SelectedTreeViewItems.HasMultipleItems())
                    {
                        if (!Item.IsSelected && down)
                            ParentTreeView.RangeSelect(this);
                        else if (Item.IsSelected && !down)
                            EnterEditModeDelayed();
                    }
                    else
                    {
                        if ((!Item.IsSelected && down) || (Item.IsSelected && down))
                            ParentTreeView.RangeSelect(this);
                    }
                }
                else if (!isShiftPressed && isCtrlPressed)
                {
                    if (!ParentTreeView.SelectedTreeViewItems.HasMultipleItems())
                    {
                        if (!Item.IsSelected && down)
                            ParentTreeView.SelectItem(this);
                        else if (Item.IsSelected && !down)
                            ParentTreeView.DeselectItem(this);
                    }
                    else
                    {
                        if (!Item.IsSelected && down)
                            ParentTreeView.SelectItem(this);
                        else if (Item.IsSelected && !down)
                            ParentTreeView.DeselectItem(this);
                    }
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if (!isShiftPressed && !isCtrlPressed)
                {
                    if (!ParentTreeView.SelectedTreeViewItems.HasMultipleItems())
                    {
                        if (!Item.IsSelected && down)
                            ParentTreeView.SelectItemExclusively(this);
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Debug.WriteLine($"Entering OnMouseDown - ClickCount = {e.ClickCount}");
            e.Handled = true; // we handle clicks

            // cature mouse to get mouse up event if mouse is realesed out of element bounds
            //Mouse.Capture(this);

            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
            {
                if (e.ClickCount == 1) // Single click
                {
                    bool wasSelected = Item.IsSelected;

                    // Let Tree select this item
                    HandleMouseClick(e, true, IsShiftPressed, IsCtrlPressed);
                    
                    // If the item was not selected before we change into pre-selection mode
                    // Aka. User clicked the item for the first time
                    if (!wasSelected && Item.IsSelected)
                    {
                        justReceivedSelection = true;
                    }
                }
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                leftMouseButtonClickCount = e.ClickCount;

                DragDrop.OnMouseDown(this, e);

                if (e.ClickCount > 1)
                {
                    // Cancel any single click action which was delayed
                    if (leftSingleClickCancelSource != null)
                    {
                        Debug.WriteLine("Cancel single click");
                        leftSingleClickCancelSource.Cancel();
                        leftSingleClickCancelSource = null;
                    }
                }
            }

            Debug.WriteLine("Leaving OnMouseDown");
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                // Don't do anything and let the magic handle the ContextMenu (for both the TextBox and TreeItems)
                return;
            }

            // Note: e.ClickCount is always 1 for MouseUp
            Debug.WriteLine($"Entering OnMouseUp");
            
            e.Handled = true; // we handle  clicks
            
            // release mouse capture
            //Mouse.Capture(null);

            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
            {
                // If we just received the selection (inside MouseDown)
                if (Item.IsSelected)
                {
                    Focus();
                }
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                // First click is special, as the only action to take is to select the item
                // Only do stuff if we're not the first click
                if (!justReceivedSelection)
                {
                    bool hasManyItemsSelected = ParentTreeView.SelectedTreeViewItems.Take(2).Count() == 2;
                    bool shouldEnterEditMode = Item.IsEditable
                            && !Item.IsInEditMode
                            && !IsCtrlPressed;

                    // Only trigger actions if we're the first click in the DoubleClick timespan
                    if (leftMouseButtonClickCount == 1)
                    {
                        HandleMouseClick(e, false, IsShiftPressed, IsCtrlPressed);
                    }
                    else if(leftMouseButtonClickCount == 2)
                    {
                        if (!IsCtrlPressed && !IsShiftPressed)
                        {
                             // Remove selection of other items
                            ParentTreeView.SelectItemExclusively(this);

                            if (Item is CmdArgument)
                            {
                                if (shouldEnterEditMode)
                                {
                                    Item.BeginEdit();
                                    Debug.WriteLine("Enter edit mode with double click");
                                }
                            }

                            if (Item is CmdContainer)
                            {
                                IsExpanded = !IsExpanded;
                                Debug.WriteLine("Toggled expanded");
                            }                           
                        }
                    }
                }

                // Item is now officially selected
                justReceivedSelection = false;
                leftMouseButtonClickCount = 0;
            }

            Debug.WriteLine($"Leaving OnMouseUp");
        }

        protected override void OnIsKeyboardFocusedChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ParentTreeView.ChangedFocusedItem(this);
            }
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is CmdBase item)
            {
                item.IsFocusedItem = (bool)e.NewValue;
            }
        }


        private void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;

            // ignore bring into view if mouse buttons are pressed, to prevent random scrolling and drag'n'drop
            if (Mouse.LeftButton == MouseButtonState.Pressed || Mouse.RightButton == MouseButtonState.Pressed)
                return;

            var scrollView = ParentTreeView.Template.FindName("_tv_scrollviewer_", ParentTreeView) as ScrollViewer;
            var scrollPresenter = scrollView.Template.FindName("PART_ScrollContentPresenter", scrollView) as ScrollContentPresenter; // ScrollViewer without scrollbars

            // If item is not fully created, finish layout
            if (this.ItemBorder == null)
            {
                UpdateLayout();
            }
            
            scrollPresenter?.MakeVisible(this, new Rect(new Point(0, 0), this.ItemBorder.RenderSize));
        }
        

        protected override void OnDragEnter(DragEventArgs e) => DragDrop.OnDragEnter(this, e);
        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e) => DragDrop.OnQueryContinueDrag(this, e);
        protected override void OnDragLeave(DragEventArgs e) => DragDrop.OnDragLeave(this, e);
        protected override void OnDrop(DragEventArgs e) => DragDrop.HandleDropForTarget(this, e);
        protected override void OnDragOver(DragEventArgs e)
        {
            DragDrop.OnDragOver(this, e);

            ScrollViewer sv = TreeHelper.FindVisualChild<ScrollViewer>(ParentTreeView);

            double tolerance = 15;
            double verticalPos = e.GetPosition(sv).Y;

            if (verticalPos < tolerance) // Top of visible list?
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset - (tolerance - verticalPos) / 2); //Scroll up.
            }
            else if (verticalPos > sv.ViewportHeight - tolerance) //Bottom of visible list?
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset + (verticalPos - sv.ViewportHeight + tolerance) / 1.5); //Scroll down.    
            }
        }

        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register(nameof(LevelProperty), typeof(int), typeof(TreeViewItemEx), new PropertyMetadata(0));
    }
}
