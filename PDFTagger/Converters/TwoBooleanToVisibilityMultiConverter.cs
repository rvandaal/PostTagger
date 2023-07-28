using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PDFTagger.Converters {
    public class TwoBooleanToVisibilityMultiConverter : IMultiValueConverter {

        /// <summary>
        /// Returns Visible if the parameter is '||' and one of the two boolean inputs is true, or 
        /// if the parameter is not '||' and both of the boolean inputs are true.
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if ((values[0] is bool) && (values[0] != null)) {
                if ((values[1] is bool) && (values[1] != null)) {
                    if (
                        parameter != null && parameter.ToString() == "||" && (((bool)values[0]) || ((bool)values[1]))
                        || (parameter == null || parameter.ToString() != "||") && ((bool)values[0]) && ((bool)values[1])
                    ) {
                        return Visibility.Visible;
                    }
                }
            }
            return Visibility.Collapsed;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            const string msg = "TwoBooleanToVisibilityMultiConverter: Not implemented ConvertBack method";
            throw new NotImplementedException(msg);
        }
    }
}

