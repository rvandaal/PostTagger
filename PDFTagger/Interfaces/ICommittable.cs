using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PDFTagger.Interfaces {
    public interface ICommittable {
        event RoutedEventHandler EnterPressed;
        event RoutedEventHandler TabPressed;
        event RoutedEventHandler ShiftTabPressed;
    }
}
