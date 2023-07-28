using System.Windows;

namespace PDFTagger.Interfaces
{
    public interface ICommittable {
        event RoutedEventHandler EnterPressed;
        event RoutedEventHandler TabPressed;
        event RoutedEventHandler ShiftTabPressed;
    }
}
