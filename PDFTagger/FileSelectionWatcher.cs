using System;
using System.Windows.Threading;
using System.Collections.Generic;

namespace PDFTagger
{
    public class FileSelectionWatcher {

        public event EventHandler Updated;

        public List<string> Selected { get; private set; }
        public List<string> PreviousSelected { get; private set; }

        public FileSelectionWatcher() {
            Selected = new List<string>();
            PreviousSelected = new List<string>();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += OnTick;
            timer.Start();
        }

        private void OnTick(object sender, EventArgs e) {
            string filename;
            PreviousSelected.Clear();
            Selected.ForEach(s => PreviousSelected.Add(s));
            Selected.Clear();
            try {
                var sw = new SHDocVw.ShellWindows();
                foreach (SHDocVw.InternetExplorer window in sw) {
                    filename = System.IO.Path.GetFileNameWithoutExtension(window.FullName).ToLower();
                    if (filename.ToLowerInvariant() == "explorer") {
                        Shell32.FolderItems items;
                        try {
                            var doc = ((Shell32.IShellFolderViewDual2)window.Document);
                            items = doc.SelectedItems();
                            foreach (Shell32.FolderItem item in items) {
                                Selected.Add(item.Path);
                            }
                        } catch (Exception) {

                        }
                    }
                }
                if (Selected.Count != PreviousSelected.Count || !Selected.TrueForAll(s => PreviousSelected.Contains(s))) {
                    RaiseUpdated();
                }
            } catch(Exception ex) {
                throw;
            }
        }

        private void RaiseUpdated() {
            Updated?.Invoke(this, null);
        }
    }
}
