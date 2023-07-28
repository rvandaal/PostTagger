using System.Windows.Input;

namespace PDFTagger.Interfaces {
    public interface IFormKeyboardFocusChanged {
        event KeyboardEventHandler KeyboardFocusForValidationChanged;
    }
}
