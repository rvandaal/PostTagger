using System.Windows;

namespace PDFTagger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            DataContext = new MainWindowViewModel();
            InitializeComponent();
            //companyAutoCompleteBox.Loaded += (sender, args) => companyAutoCompleteBox.Focus();
            //Loaded += (sender, args) => InfoBalloon.RegisterForAutomaticEmptyTextValidation(companyAutoCompleteBox, () => companyAutoCompleteBox.Text, dialogSubPage);
            //Unloaded += (sender, args) => InfoBalloon.UnregisterFromAutomaticEmptyTextValidation(dialogSubPage);
        }

        private void OnCompanyCommitted(object sender, RoutedEventArgs e) {
            dateAutoCompleteBox.Focus();
        }

        private void OnDateCommitted(object sender, RoutedEventArgs e) {
            descriptionAutoCompleteBox.Focus();
        }

        private void OnDescriptionCommitted(object sender, RoutedEventArgs e) {
            saveButton.Focus();
        }
    }
}
