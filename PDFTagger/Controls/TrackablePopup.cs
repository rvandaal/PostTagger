using System;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;

namespace PDFTagger.Controls {
    /// <summary>
    /// Custom Popup that keeps track of all open Popups.
    /// </summary>
    public class TrackablePopup : Popup {

        static TrackablePopup() {
            OpenPopups = new List<TrackablePopup>();
        }

        public static List<TrackablePopup> OpenPopups { get; private set; }

        protected override void OnOpened(EventArgs e) {
            base.OnOpened(e);
            if (!OpenPopups.Contains(this)) {
                OpenPopups.Add(this);
            }
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            if (OpenPopups.Contains(this)) {
                OpenPopups.Remove(this);
            }
        }
    }
}
