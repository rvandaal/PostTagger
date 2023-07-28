using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using PDFTagger.Interfaces;

namespace PDFTagger.Controls {
    public class InfoBalloon : ContentControl {

        private Popup popup;

        private sealed class RegisteredElementForEmptyTextValidation {
            public FrameworkElement Element { get; }
            public IFormKeyboardFocusChanged FocusScopeElement { get; }
            public Func<string> TextFunction { get; }
            public bool GotFocusOnce { get; set; }

            public RegisteredElementForEmptyTextValidation(FrameworkElement element, IFormKeyboardFocusChanged form, Func<string> textFunction, bool ignoreFocus = false) {
                Element = element;
                FocusScopeElement = form;
                TextFunction = textFunction;
                GotFocusOnce = ignoreFocus;
            }
        }

        private static readonly List<RegisteredElementForEmptyTextValidation> RegisteredElementsForEmptyTextValidation = 
            new List<RegisteredElementForEmptyTextValidation>();

        #region Constructors

        static InfoBalloon() {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(InfoBalloon),
                new FrameworkPropertyMetadata(typeof(InfoBalloon))
            );
        }

        #endregion

        #region Public properties

        #region Placement

        public static PlacementMode GetPlacement(
            DependencyObject dependencyObject
        ) {
            return (PlacementMode)dependencyObject.GetValue(PlacementProperty);
        }

        public static void SetPlacement(
            DependencyObject dependencyObject,
            PlacementMode value
        ) {
            dependencyObject.SetValue(PlacementProperty, value);
        }

        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.RegisterAttached(
                "Placement",
                typeof(PlacementMode),
                typeof(InfoBalloon),
                new FrameworkPropertyMetadata(
                    PlacementMode.Right,
                    FrameworkPropertyMetadataOptions.Inherits
                )
            );

        #endregion

        #region HorizontalPlacementTarget

        public static FrameworkElement GetHorizontalPlacementTarget(
            DependencyObject dependencyObject
        ) {
            return dependencyObject.GetValue(HorizontalPlacementTargetProperty) as FrameworkElement;
        }

        public static void SetHorizontalPlacementTarget(
            DependencyObject dependencyObject,
            FrameworkElement value
        ) {
            dependencyObject.SetValue(HorizontalPlacementTargetProperty, value);
        }

        public static readonly DependencyProperty HorizontalPlacementTargetProperty =
            DependencyProperty.RegisterAttached(
                "HorizontalPlacementTarget",
                typeof(FrameworkElement),
                typeof(InfoBalloon),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnPlacementTargetChanged
                )
            );

        #endregion

        #region VerticalPlacementTarget

        public static FrameworkElement GetVerticalPlacementTarget(
            DependencyObject dependencyObject
        ) {
            return dependencyObject.GetValue(VerticalPlacementTargetProperty) as FrameworkElement;
        }

        public static void SetVerticalPlacementTarget(
            DependencyObject dependencyObject,
            FrameworkElement value
        ) {
            dependencyObject.SetValue(VerticalPlacementTargetProperty, value);
        }

        public static readonly DependencyProperty VerticalPlacementTargetProperty =
            DependencyProperty.RegisterAttached(
                "VerticalPlacementTarget",
                typeof(FrameworkElement),
                typeof(InfoBalloon),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnPlacementTargetChanged
                )
            );

        #endregion

        #region IsOpen

        public static bool GetIsOpen(DependencyObject dependencyObject) {
            return (bool)dependencyObject.GetValue(IsOpenProperty);
        }

        public static void SetIsOpen(DependencyObject dependencyObject, bool value) {
            dependencyObject.SetValue(IsOpenProperty, value);
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.RegisterAttached(
                "IsOpen",
                typeof(bool),
                typeof(InfoBalloon),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Inherits
                )
            );

        #endregion

        #region HorizontalOffset

        public double HorizontalOffset {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        public static readonly DependencyProperty HorizontalOffsetProperty =
            Popup.HorizontalOffsetProperty.AddOwner(typeof(InfoBalloon));

        #endregion

        #region VerticalOffset

        public double VerticalOffset {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        public static readonly DependencyProperty VerticalOffsetProperty =
            Popup.VerticalOffsetProperty.AddOwner(typeof(InfoBalloon));

        #endregion

        #endregion

        #region Public methods

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            popup = Template.FindName("PART_Popup", this) as Popup;
            SubscribeToCustomPopupPlacementCallback();
        }

        public static void RegisterForAutomaticEmptyTextValidation(
            FrameworkElement element, 
            Func<string> textFunc, 
            IFormKeyboardFocusChanged form, 
            bool ignoreFocus = false
        ) {
            if (form != null && RegisteredElementsForEmptyTextValidation.All(s => s.FocusScopeElement != form)) {
                form.KeyboardFocusForValidationChanged += OnKeyboardFocusChanged;
            }
            RegisterForEmptyTextValidation(element, textFunc, form, ignoreFocus);
        }

        public static void RegisterForEmptyTextValidation(
            FrameworkElement element, 
            Func<string> textFunc, 
            IFormKeyboardFocusChanged form, 
            bool ignoreFocus = false
        ) {
            if (RegisteredElementsForEmptyTextValidation.All(s => s.Element != element)) {
                RegisteredElementsForEmptyTextValidation.Add(
                    new RegisteredElementForEmptyTextValidation(element, form, textFunc, ignoreFocus)
                );
            }
        }

        public static void UnregisterFromAutomaticEmptyTextValidation(IFormKeyboardFocusChanged form) {
            UnregisterFromEmptyTextValidation(form);
            form.KeyboardFocusForValidationChanged -= OnKeyboardFocusChanged;
        }

        public static void UnregisterFromEmptyTextValidation(IFormKeyboardFocusChanged form) {
            List<RegisteredElementForEmptyTextValidation> elementsToRemove =
                RegisteredElementsForEmptyTextValidation.Where(s => s.FocusScopeElement == form).ToList();
            //
            // Notice that all this code controls the attached property InfoBalloon.IsOpen on autocompleteboxes, 
            // textboxes etc that registered.
            // Don't forget to reset this property when unregistering.
            // 
            elementsToRemove.ForEach(e => e.Element.ClearValue(IsOpenProperty));
            elementsToRemove.ForEach(e => RegisteredElementsForEmptyTextValidation.Remove(e));
        }

        public static void UpdateIsOpen(object originalSource, UIElement focusScopeElement) {
            foreach (
                var s in RegisteredElementsForEmptyTextValidation.Where(
                    r => focusScopeElement == null || r.FocusScopeElement == focusScopeElement
                )
            ) {
                if (originalSource == s.Element) {
                    s.Element.ClearValue(IsOpenProperty);
                    s.GotFocusOnce = true;
                } else if (
                    (s.GotFocusOnce || originalSource is Button && ((Button)originalSource).IsDefault) &&
                    Validation.GetHasError(s.Element) &&
                    string.IsNullOrEmpty(s.TextFunction.Invoke())
                ) {
                    SetIsOpen(s.Element, true);
                }
            }
        }

        #endregion

        #region Private methods

        private static void OnKeyboardFocusChanged(object sender, KeyboardEventArgs e) {
            UpdateIsOpen(e.OriginalSource as UIElement, null);
        }

        private static void OnPlacementTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            InfoBalloon infoBalloon = d as InfoBalloon;
            infoBalloon?.SubscribeToCustomPopupPlacementCallback();
        }

        private void SubscribeToCustomPopupPlacementCallback() {
            if (popup != null && GetHorizontalPlacementTarget(this) != null && GetVerticalPlacementTarget(this) != null) {
                popup.PlacementTarget = GetVerticalPlacementTarget(this);
                popup.Placement = PlacementMode.Custom;
                if (GetVerticalPlacementTarget(this).IsLoaded) {
                    OnLoaded(null, null);
                } else {
                    GetVerticalPlacementTarget(this).Loaded += OnLoaded;
                }
            }
        }

        private void OnLoaded(object sender, EventArgs e) {
            Debug.Assert(
                GetPlacement(this) == PlacementMode.Left ||
                GetPlacement(this) == PlacementMode.Top ||
                GetPlacement(this) == PlacementMode.Right ||
                GetPlacement(this) == PlacementMode.Bottom
            );
            if (popup != null && GetHorizontalPlacementTarget(this) != null && GetVerticalPlacementTarget(this) != null) {
                popup.CustomPopupPlacementCallback += OnCustomPopupPlacementCallback;
            }
        }

        protected virtual CustomPopupPlacement[] OnCustomPopupPlacementCallback(Size size, Size targetSize, Point offset) {
            if (GetHorizontalPlacementTarget(this) != null && GetVerticalPlacementTarget(this) != null) {
                CustomPopupPlacement placement;

                switch (GetPlacement(this)) {
                    case PlacementMode.Left:
                        placement =
                            new CustomPopupPlacement(
                                new Point(-size.Width, (targetSize.Height - size.Height) / 2),
                                PopupPrimaryAxis.Horizontal
                            );
                        break;
                    case PlacementMode.Top:
                        placement =
                            new CustomPopupPlacement(
                                new Point((targetSize.Width - size.Width) / 2, -size.Height),
                                PopupPrimaryAxis.Vertical
                            );
                        break;
                    case PlacementMode.Bottom:
                        placement =
                            new CustomPopupPlacement(
                                new Point((targetSize.Width - size.Width) / 2, targetSize.Height),
                                PopupPrimaryAxis.Vertical
                            );
                        break;
                    default:
                        placement =
                            new CustomPopupPlacement(
                                new Point(targetSize.Width, (targetSize.Height - size.Height) / 2),
                                PopupPrimaryAxis.Horizontal
                            );
                        break;

                }
                return
                    new[] { placement };
            }
            return new CustomPopupPlacement[] { };
            //
            // PR2973 STU3: On Windows10, with DPI=300% and custom textsize = 300% (this is one example), the infoballoons didn't appear
            // at the right of their HorizontalPlacementTarget anymore, they appeared somewhere in the middle, thereby covering their target.
            // Because of this, using ActualWidth was not an option anymore.
            // That's why the code below is simplified (see new code above): the target of the infoballoon is assumed to be one control (PlacementTarget = VerticalPlacementTarget).
            // Because of this, we can use targetSize.Width iso ActualWidth and this solves the PR. targetSize refers to the PlacementTarget's size.
            // The only place where this changes things is the taskpanel: the infoballoons of the spinners on the taskpanel were horizontally aligned
            // with the taskpanel. With the new code, infoballoons will be horizontally aligned with the spinners themselves.
            // This is even better because in the PathPlanning task, spinners exist next to each other. If one of the infoballoons were aligned
            // with the taskpanel iso the spinner, it could not be determined which spinner had the error.
            // So in short: new code = better.
            //
            // old code:

            //if (GetHorizontalPlacementTarget(this) != null && GetVerticalPlacementTarget(this) != null) {
            //    Point p;
            //    CustomPopupPlacement placement;

            //    switch (GetPlacement(this)) {
            //        case PlacementMode.Left:
            //            p = GetHorizontalPlacementTarget(this).TranslatePoint(
            //                new Point(-size.Width, 0),
            //                GetVerticalPlacementTarget(this)
            //            );
            //            placement =
            //                new CustomPopupPlacement(
            //                    new Point(p.X, (targetSize.Height - size.Height) / 2),
            //                    PopupPrimaryAxis.Horizontal
            //                );
            //            break;
            //        case PlacementMode.Top:
            //            p = GetVerticalPlacementTarget(this).TranslatePoint(
            //                new Point(0, -size.Height),
            //                GetHorizontalPlacementTarget(this)
            //            );
            //            placement =
            //                new CustomPopupPlacement(
            //                    new Point((targetSize.Width - size.Width) / 2, p.Y),
            //                    PopupPrimaryAxis.Vertical
            //                );
            //            break;
            //        case PlacementMode.Bottom:
            //            p = GetVerticalPlacementTarget(this).TranslatePoint(
            //                new Point(0, GetVerticalPlacementTarget(this).ActualHeight),
            //                GetHorizontalPlacementTarget(this)
            //            );
            //            placement =
            //                new CustomPopupPlacement(
            //                    new Point((targetSize.Width - size.Width) / 2, p.Y),
            //                    PopupPrimaryAxis.Vertical
            //                );
            //            break;
            //        default:
            //            p = GetHorizontalPlacementTarget(this).TranslatePoint(
            //                new Point(GetHorizontalPlacementTarget(this).ActualWidth, 0),
            //                GetVerticalPlacementTarget(this)
            //            );
            //            placement =
            //                new CustomPopupPlacement(
            //                    new Point(p.X, (targetSize.Height - size.Height) / 2),
            //                    PopupPrimaryAxis.Horizontal
            //                );
            //            break;

            //    }
            //    return
            //        new[] { placement };
            //}
            //return new CustomPopupPlacement[] { };
        }

        #endregion

    }
}
