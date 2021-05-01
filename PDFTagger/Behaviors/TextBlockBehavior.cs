using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PDFTagger.Behaviors {
    public class TextBlockBehavior {

        //
        // Source: http://blog.scottlogic.com/2011/01/31/automatically-showing-tooltips-on-a-trimmed-textblock-silverlight-wpf.html
        //
        public static bool GetHasAutoToolTip(DependencyObject obj) {
            return (bool)obj.GetValue(HasAutoToolTipProperty);
        }

        public static void SetHasAutoToolTip(DependencyObject obj, bool value) {
            obj.SetValue(HasAutoToolTipProperty, value);
        }

        public static readonly DependencyProperty HasAutoToolTipProperty =
            DependencyProperty.RegisterAttached(
                "HasAutoToolTip",
                typeof(bool),
                typeof(TextBlockBehavior),
                new PropertyMetadata(false, OnHasAutoToolTipPropertyChanged)
            );

        private static void OnHasAutoToolTipPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            TextBlock textBlock = d as TextBlock;
            if (textBlock == null)
                return;

            if (e.NewValue.Equals(true)) {
                textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
                //
                // These are a lot of subscriptions for all text blocks in the application.
                // But the performance impact seems to be minimal.
                // We should keep on monitoring the performance; for now, the 
                // subscriptions below provide all possible triggers that are needed to 
                // update the tooltip on a truncated textblock.
                //
                textBlock.Loaded += OnPropertyChanged;
                textBlock.SizeChanged += OnPropertyChanged;
                //
                // The text could change while the size doesn't change, but the tooltip still
                // has to be updated, so we need this event to see any change in a databound
                // TextBlock.Text. NOTE: the corresponding binding on the Text property should
                // set: NotifyOnTargetUpdated=True. This can be done on a per case basis.
                //
                textBlock.TargetUpdated += OnPropertyChanged;

                if (textBlock.IsLoaded) {
                    ComputeAutoToolTip(textBlock);
                }
            } else {
                textBlock.ClearValue(TextBlock.TextTrimmingProperty);
                textBlock.ClearValue(ToolTipService.ToolTipProperty);
                textBlock.ClearValue(ToolTipService.ShowOnDisabledProperty);
                textBlock.Loaded -= OnPropertyChanged;
                textBlock.SizeChanged -= OnPropertyChanged;
                textBlock.TargetUpdated -= OnPropertyChanged;
                textBlock.Tag = null;
            }
        }

        private static void OnPropertyChanged(object sender, RoutedEventArgs e) {
            ComputeAutoToolTip((TextBlock)sender);
        }

        private static void ComputeAutoToolTip(TextBlock textBlock) {
            //
            // If the text block is not loaded, it doesn't make sense to check the ActualWidth.
            //
            if (!textBlock.IsLoaded) {
                return;
            }
            //
            // A ToolTip should be generated if it was not set locally by the user. Hence, if both:
            // 1. The ToolTip is not data bound in the xaml (by the user)
            // 2. The ToolTip is generated before OR [it is null (i.e. not set by the user)]
            //
            if (
                textBlock.Tag != null ||
                !(BindingOperations.IsDataBound(textBlock, FrameworkElement.ToolTipProperty) && textBlock.ToolTip != null)
            ) {
                textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var width = textBlock.DesiredSize.Width - textBlock.Margin.Left - textBlock.Margin.Right;
                ToolTipService.SetToolTip(textBlock, textBlock.ActualWidth < width ? textBlock.Text : null);
                ToolTipService.SetShowOnDisabled(textBlock, true);
                //
                // Ok ok, let's use a tag to indicate the tooltip was generated (and thus, could be not-null).
                // Don't use some cached list with TextBlocks because this will lead to memory leaks.
                //
                textBlock.Tag = new object();
            }
        }
    }
}
