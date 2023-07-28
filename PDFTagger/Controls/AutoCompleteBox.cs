using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PDFTagger.Interfaces;

namespace PDFTagger.Controls
{
    public class AutoCompleteBox : TextBox, ICommittable {

        #region Private fields

        private Selector selector;
        private bool isTextChangedByCode;
        private bool isSelectionChangedByNewText;
        private bool isMouseDownWhileDropDownIsClosed;
        private bool shouldDropDownOpenAfterFocus = true;
        private bool isSelectorLoaded;
        private ICollectionView collectionView;

        #endregion

        #region Private properties
        private string customEntry;
        private string CustomEntry {
            set {
                if (customEntry != value) {
                    if (string.IsNullOrEmpty(customEntry) && !string.IsNullOrEmpty(value)) {
                        TextItemsSource.Insert(0, value);
                    } else if (!string.IsNullOrEmpty(customEntry) && string.IsNullOrEmpty(value)) {
                        TextItemsSource.RemoveAt(0);
                    } else if (!string.IsNullOrEmpty(customEntry) && !string.IsNullOrEmpty(value)) {
                        TextItemsSource[0] = value;
                    }
                    customEntry = value;
                }
            }
        }
        #endregion

        #region Routed events

        public event RoutedEventHandler EnterPressed {
            add { AddHandler(EnterPressedEvent, value); }
            remove { RemoveHandler(EnterPressedEvent, value); }
        }

        public static readonly RoutedEvent EnterPressedEvent =
            EventManager.RegisterRoutedEvent(
                "EnterPressed",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(AutoCompleteBox)
            );

        public event RoutedEventHandler TabPressed {
            add { AddHandler(TabPressedEvent, value); }
            remove { RemoveHandler(TabPressedEvent, value); }
        }

        public static readonly RoutedEvent TabPressedEvent =
            EventManager.RegisterRoutedEvent(
                "TabPressed",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(AutoCompleteBox)
            );

        public event RoutedEventHandler ShiftTabPressed {
            add { AddHandler(ShiftTabPressedEvent, value); }
            remove { RemoveHandler(ShiftTabPressedEvent, value); }
        }

        public static readonly RoutedEvent ShiftTabPressedEvent =
            EventManager.RegisterRoutedEvent(
                "ShiftTabPressed",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(AutoCompleteBox)
            );

        private void RaiseEnterPressedEvent() {
            var eventArgs = new RoutedEventArgs(EnterPressedEvent);
            RaiseEvent(eventArgs);
        }

        private void RaiseTabPressedEvent() {
            var eventArgs = new RoutedEventArgs(TabPressedEvent);
            RaiseEvent(eventArgs);
        }

        private void RaiseShiftTabPressedEvent() {
            var eventArgs = new RoutedEventArgs(ShiftTabPressedEvent);
            RaiseEvent(eventArgs);
        }

        #endregion

        #region Dependency properties

        #region TextItemsSource
        public ObservableCollection<string> TextItemsSource {
            get { return (ObservableCollection<string>)GetValue(TextItemsSourceProperty); }
            set { SetValue(TextItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty TextItemsSourceProperty =
            DependencyProperty.Register(
                "TextItemsSource",
                typeof(ObservableCollection<string>),
                typeof(AutoCompleteBox),
                new FrameworkPropertyMetadata(null, OnTextItemsSourceChanged)
            );
        #endregion

        #region IsDropDownOpen
        public bool IsDropDownOpen {
            get { return (bool)GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        public static readonly DependencyProperty IsDropDownOpenProperty =
            ComboBox.IsDropDownOpenProperty.AddOwner(
                typeof(AutoCompleteBox),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );
        #endregion

        #region IsCaseSensitive
        public bool IsCaseSensitive {
            get { return (bool)GetValue(IsCaseSensitiveProperty); }
            set { SetValue(IsCaseSensitiveProperty, value); }
        }

        public static readonly DependencyProperty IsCaseSensitiveProperty =
            DependencyProperty.Register(
                "IsCaseSensitive",
                typeof(bool),
                typeof(AutoCompleteBox),
                new FrameworkPropertyMetadata(false)
            );
        #endregion

        #endregion

        #region Constructors

        static AutoCompleteBox() {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AutoCompleteBox), 
                new FrameworkPropertyMetadata(typeof(AutoCompleteBox))
            );
        }

        public AutoCompleteBox() {
            AddHandler(MouseLeftButtonDownEvent, new RoutedEventHandler(OnMouseLeftButtonDown), true);
            DataContextChanged += OnDataContextChanged;
        }

        #endregion

        #region Public methods

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if(selector != null) {
                selector.SelectionChanged -= OnSelectionChanged;
                selector.Loaded -= OnSelectorLoaded;
                selector.Unloaded -= OnSelectorUnloaded;
            }
            selector = Template.FindName("PART_Selector", this) as Selector;
            if (selector != null) {
                selector.SelectionChanged += OnSelectionChanged;
                selector.Loaded += OnSelectorLoaded;
                selector.Unloaded += OnSelectorUnloaded;
            }
        }

        #endregion

        #region Protected methods
        
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) {
            base.OnGotKeyboardFocus(e);
            //
            // On keyboard focus, we want the dropdown to open, except when the focus returns after an item was clicked. 
            // Immediately after opening, the PopupRoot will
            // capture the mouse, and only when it has mouse capture, it will close itself when the user clicks outside the dropdown.
            // However, if we open the dropdown directly in this GotKeyboardFocus handler, the following happens
            // when the user focusses the AutoCompleteBox with a mouse click:
            // 1. A MouseButtonDown event is handled by the TextEditorMouse.OnMouseDown handler.
            // 2.     This will first put the keyboard focus on the AutoCompleteBox
            // 3.     The GotKeyboardFocus event is raised and the dropdown is opened by this handler. The popuproot captures the mouse.
            // 4.     The AutoCompleteBox captures the mouse, so the PopupRoot will lose it. The MS code lets the 
            //        AutoCompleteBox capture the mouse to allow for text selection and drag&drop of text.
            // 5. A MouseUp event occurs.
            // 6. The popup will not be closed anymore when the user clicks outside the popup.
            //
            // Because of this, we have to postpone opening the popup after a MouseUp event: then it will steal
            // the mouse capture from the AutoCompleteBox. Possible textselection and drag&drop operations are ended anyway.
            // To be exact, we have to use the PreviewMouseUp, since the MouseUp event is already handled by a TextBox classhandler.
            //
            // In case the AutoCompleteBox gets focus programmatically, then there is no PreviewMouseUp event and the AutoCompleteBox
            // will not capture the mouse. In this situation we have to open the dropdown immediately on GotKeyboardFocus.
            //
            // Also, we have to check the original source; if an item in the list is clicked, that item will get focus, that GotKeyboardFocus event
            // will bubble upto this handler, but we only want to do stuff if the original source is the AutoCompleteBox itself.
            //
            if (e.OriginalSource == this) {
                if (shouldDropDownOpenAfterFocus) {
                    if (isMouseDownWhileDropDownIsClosed) {
                        PreviewMouseUp += OnPreviewMouseUp;
                    }
                    else {
                        ResetSelection();
                        OpenDropDown();
                    }
                }
                shouldDropDownOpenAfterFocus = true;
                SelectAllText();
            }
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {
            base.OnLostKeyboardFocus(e);
            if (!IsKeyboardFocusWithin) {
                CloseDropDown();
            }
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnPreviewMouseLeftButtonDown(e);
            isMouseDownWhileDropDownIsClosed = !IsDropDownOpen;
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e) {
            //
            // This handler handles the case in which the user clicks the textbox while it already has focus.
            // It could be that the dropdown is closed; on every first click we want it to open (not on a doubleclick).
            //
            if (IsKeyboardFocusWithin && e.ClickCount == 1 && e.ChangedButton == MouseButton.Left) {
                OpenDropDown();
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e) {
            base.OnPreviewKeyDown(e);
            switch (e.Key) {
                case Key.Escape:
                    if(IsDropDownOpen) {
                        Text = "";
                        CloseDropDown();
                        e.Handled = true;
                    }
                    break;
                case Key.Enter:
                    if (!string.IsNullOrEmpty(Text)) {
                        if(IsDropDownOpen) {
                            CloseDropDown();
                        }
                        RaiseCommittedEvent();
                    }
                    //
                    // We handle the Enter key, because in general, pressing Enter on an AutoCompleteBox will put the focus
                    // on the next field.
                    //
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (!IsDropDownOpen) {
                        OpenDropDown();
                    } else if (collectionView != null) {
                        collectionView.MoveCurrentToNext();
                        if (collectionView.IsCurrentAfterLast) {
                            collectionView.MoveCurrentToLast();
                        }
                    }
                    e.Handled = true;
                    break;
                case Key.Up:
                    if (IsDropDownOpen && collectionView != null) {
                        collectionView.MoveCurrentToPrevious();
                        if (collectionView.IsCurrentBeforeFirst) {
                            collectionView.MoveCurrentToFirst();
                        }
                    }
                    e.Handled = true;
                    break;
            }
        }


        protected override void OnTextChanged(TextChangedEventArgs e) {
            base.OnTextChanged(e);
            //
            // If the text is changed because some item was selected, then don't open the dropdown.
            //
            if (isTextChangedByCode) {
                return;
            }
            isSelectionChangedByNewText = true;
            if (selector != null) {
                selector.SelectedIndex = -1;
            }
            if (collectionView == null) {
                isSelectionChangedByNewText = false;
                return;
            }
            //
            // Refresh to perform filtering
            //
            collectionView.Refresh();

            SelectOrCreateMatchingItem();
            OpenDropDown();
            isSelectionChangedByNewText = false;
        }

        #endregion

        #region Private methods

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            CloseDropDown();
        }

        private static void OnMouseLeftButtonDown(object sender, RoutedEventArgs e) {
            AutoCompleteBox autoCompleteBox = (AutoCompleteBox)sender;
            if (autoCompleteBox.IsDropDownOpen) {
                //
                // This code handles the case in which a selection was being made with the arrow keys and the user
                // clicked the current selected item after that. We cannot close the dropdown in the preview
                // mousedown, because then the item will not receive a bubbling mousedown and the item will
                // never be selected in the case that it was clicked with the mouse.
                //
                // This eventhandler is registered with handleEventsToo=true because the item will handle the 
                // MouseLeftButtonDown event and that handler will come first.
                //
                Visual dependencyObject = e.OriginalSource as Visual;
                if (dependencyObject.FindAncestor<ListBoxItem>() != null) {
                    autoCompleteBox.CloseDropDown();
                    //
                    // Focus back on the textbox, without opening the dropdown
                    //
                    autoCompleteBox.shouldDropDownOpenAfterFocus = false;
                    autoCompleteBox.Focus();
                    autoCompleteBox.RaiseCommittedEvent();
                    e.Handled = true;
                }
            }
        }

        private static void OnTextItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            AutoCompleteBox autoComplete = (AutoCompleteBox)d;
            autoComplete.OnTextItemsSourceChanged();
        }

        private void OnTextItemsSourceChanged() {
            TextItemsSource.CollectionChanged += TextItemsSource_CollectionChanged;
            UpdateCollectionView();        
        }

        private void TextItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            UpdateCollectionView();
        }

        private void UpdateCollectionView() {
            collectionView = CollectionViewSource.GetDefaultView(TextItemsSource);

            if (collectionView != null) {
                collectionView.Filter = item => {
                    if (string.IsNullOrEmpty(Text)) {
                        return true;
                    }
                    string itemString = (string)item;
                    string text = Text;
                    if (!IsCaseSensitive) {
                        itemString = itemString.ToLower();
                        text = text.ToLower();
                    }
                    //
                    // 1. Sort the search words on their length (descending)
                    // 2. If there are multiple search words, the longest one exactly has to match with one of the item words
                    // 3. The rest may match partly with one of the item words
                    //
                    string[] itemWords = itemString.Split(
                            new[] { ' ' },
                            StringSplitOptions.RemoveEmptyEntries
                        );
                    List<int> matchedItemWordIndices = new List<int>();
                    List<string> searchWords = text.Split(
                        new[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries
                    ).OrderByDescending(s => s.Length).ToList();
                    bool first = true;
                    foreach (string searchWord in searchWords) {
                        bool isMatchedAgainstItemWord = false;
                        bool hasToMatchExactly = first && searchWords.Count > 1;
                        first = false;

                        for (int i = 0; i < itemWords.Count(); i++) {
                            string itemWord = itemWords[i];
                            if (matchedItemWordIndices.Contains(i)) {
                                continue;
                            }
                            if (hasToMatchExactly) {
                                if (searchWord == itemWord) {
                                    matchedItemWordIndices.Add(i);
                                    isMatchedAgainstItemWord = true;
                                    break;
                                }
                            } else if (itemWord.Contains(searchWord)) {
                                matchedItemWordIndices.Add(i);
                                isMatchedAgainstItemWord = true;
                                break;
                            }
                        }
                        if (!isMatchedAgainstItemWord) {
                            //
                            // No match
                            //
                            return false;
                        }
                    }
                    return true;
                };
            }
        }

        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e) {
            PreviewMouseUp -= OnPreviewMouseUp;
            isMouseDownWhileDropDownIsClosed = false;
            ResetSelection();
            OpenDropDown();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            base.OnSelectionChanged(e);
            if (selector != null && selector.SelectedIndex > -1 && isSelectorLoaded) {
                isTextChangedByCode = true;
                if (!isSelectionChangedByNewText) {
                    Text = (string) selector.SelectedItem;
                    CaretIndex = Text.Length;
                }
                isTextChangedByCode = false;
            }
        }

        private void OnSelectorLoaded(object sender, EventArgs e) {
            isSelectorLoaded = true;
        }
        private void OnSelectorUnloaded(object sender, EventArgs e) {
            isSelectorLoaded = false;
        }

        private void OpenDropDown() {
            if (collectionView != null) {
                collectionView.Refresh();
                if (!IsDropDownOpen && TextItemsSource != null && !collectionView.IsEmpty) {
                    Dispatcher.BeginInvoke(
                        new Action(() => {
                            if (IsKeyboardFocusWithin && collectionView != null && !collectionView.IsEmpty) {
                                SetCurrentValue(IsDropDownOpenProperty, true);
                            }
                        }),
                        DispatcherPriority.Input
                    );
                }
            }
        }

        private void CloseDropDown() {
            SetCurrentValue(IsDropDownOpenProperty, false);
            if(DataContext != null) {
                //
                // If we don't check the DataContext for null, setting the Text property will
                // trigger another data validation in the viewmodel, which is already disposed. 
                // In here, the controller is used but it is already null, so an exception occurs.
                // For that reason, we check for DataContext first.
                //
                SetCurrentValue(TextProperty, Text.Trim());    
            }
            
        }

        private void SelectAllText() {
            Dispatcher.BeginInvoke(new Action(SelectAll), DispatcherPriority.Input);
        }

        private void SelectOrCreateMatchingItem() {
            if (!SelectMatchingItem()) {
                CreateMatchingItem();
            }
        }

        private bool SelectMatchingItem() {
            for (int i = 0; i < TextItemsSource.Count; i++ ) {
                string item = TextItemsSource[i];
                if (
                    (string.IsNullOrEmpty(customEntry) || i > 0) && 
                    (IsCaseSensitive ? item == Text.Trim() : item.ToLower() == Text.Trim().ToLower())
                ) {
                    CustomEntry = "";
                    if (selector != null) {
                        selector.SelectedItem = item;
                    }
                    return true;
                }
            }
            return false;
        }

        private void CreateMatchingItem() {
            if (!string.IsNullOrEmpty(Text.Trim())) {
                CustomEntry = Text.Trim();
                if (selector != null) {
                    selector.SelectedItem = Text.Trim();
                }
            } else {
                CustomEntry = null;
                if(TextItemsSource.Count == 0) {
                    CloseDropDown();
                }
            }
        }

        private void ResetSelection() {
            if(selector != null && string.IsNullOrEmpty(Text)) {
                selector.SelectedIndex = -1;
            }
        }

        private void RaiseCommittedEvent() {
            var eventArgs = new RoutedEventArgs(EnterPressedEvent);
            RaiseEvent(eventArgs);
        }

        #endregion
    }
}