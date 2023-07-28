using System;

namespace PDFTagger {
    /// <summary>
    /// Template class for quickly defining custom EventArg subclass to be used as event arguments.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventArg<T> : EventArgs {
        public T Value;

        public EventArg(T value) {
            Value = value;
        }

        public static EventArg<T> Create(T value) {
            return new EventArg<T>(value);
        }
    }
}