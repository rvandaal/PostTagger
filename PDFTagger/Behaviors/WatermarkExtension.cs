using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PDFTagger.Behaviors {
    public static class WatermarkExtension {
        #region Dependency Properties

        #region IsWatermarkVisible (ReadOnly)

        public static bool GetIsWatermarkVisible(DependencyObject dependencyObject) {
            return (bool)dependencyObject.GetValue(IsWatermarkVisibleProperty);
        }

        private static void SetIsWatermarkVisible(DependencyObject dependencyObject, bool value) {
            dependencyObject.SetValue(IsWatermarkVisiblePropertyKey, value);
        }

        private static readonly DependencyPropertyKey IsWatermarkVisiblePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsWatermarkVisible",
                typeof(bool),
                typeof(WatermarkExtension),
                new FrameworkPropertyMetadata(false, OnIsWatermarkVisibleChanged)
            );

        public static readonly DependencyProperty IsWatermarkVisibleProperty =
            IsWatermarkVisiblePropertyKey.DependencyProperty;

        #endregion

        #region WatermarkText

        public static object GetWatermarkText(DependencyObject dependencyObject) {
            return dependencyObject.GetValue(WatermarkTextProperty);
        }

        public static void SetWatermarkText(DependencyObject dependencyObject, string text) {
            dependencyObject.SetValue(WatermarkTextProperty, text);
        }

        public static readonly DependencyProperty WatermarkTextProperty =
            DependencyProperty.RegisterAttached(
                "WatermarkText",
                typeof(string),
                typeof(WatermarkExtension),
                new PropertyMetadata("WatermarkText", OnWatermarkTextChanged)
            );

        #endregion

        #endregion

        #region Private methods

        private static void OnIsWatermarkVisibleChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs
        ) {
            EnsureCorrectControl(dependencyObject);
        }

        private static void OnWatermarkTextChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e
        ) {
            EnsureCorrectControl(dependencyObject);
            Control control = dependencyObject as Control;
            if (control != null) {
                SetWatermarkVisibility(control);
                control.RemoveHandler(
                    TextBoxBase.TextChangedEvent,
                    new TextChangedEventHandler(OnTextChanged)
                );
                control.AddHandler(
                    TextBoxBase.TextChangedEvent,
                    new TextChangedEventHandler(OnTextChanged),
                    true
                );
                Selector selector = control as Selector;
                if (selector != null) {
                    selector.AddHandler(Selector.SelectionChangedEvent,
                        new SelectionChangedEventHandler(OnSelectionChanged)
                    );
                }
            }
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs e) {
            SetWatermarkVisibility(sender);
        }

        private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            SetWatermarkVisibilityForComboBox(sender as ComboBox);
        }

        private static void EnsureCorrectControl(DependencyObject dependencyObject) {
            if (!(dependencyObject is TextBox || dependencyObject is ComboBox)) {
                throw new ArgumentException(
                    // ReSharper disable LocalizableElement
                    "Control should be of type TextBox, AutoCompleteBox or ComboBox",
                    // ReSharper restore LocalizableElement
                    "dependencyObject"
                );
            }
        }

        private static void SetWatermarkVisibility(object sender) {
            SetWatermarkVisibilityForTextBox(sender as TextBox);
            SetWatermarkVisibilityForComboBox(sender as ComboBox);
        }

        private static void SetWatermarkVisibilityForTextBox(TextBox textBox) {
            if (textBox != null) {
                bool visible = string.IsNullOrEmpty(textBox.Text);
                SetIsWatermarkVisible(textBox, visible);
            }
        }

        private static void SetWatermarkVisibilityForComboBox(ComboBox comboBox) {
            if (comboBox != null) {
                bool visible = comboBox.IsEditable && string.IsNullOrEmpty(comboBox.Text) || !comboBox.IsEditable && comboBox.SelectedIndex == -1;
                SetIsWatermarkVisible(comboBox, visible);
            }
        }

        #endregion
    }
}
