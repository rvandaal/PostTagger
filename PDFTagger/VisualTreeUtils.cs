using System.Windows;
using System.Windows.Media;

namespace PDFTagger {

    public static class VisualTreeUtils {

        /// <summary>
        /// Finds a Child of a given item in the visual tree recursively. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first child item that matches the submitted type parameter. 
        /// If no matching item can be found, null is being returned.</returns>
        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject {

            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++) {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null) {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                } else if (!string.IsNullOrEmpty(childName)) {
                    FrameworkElement frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName) {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                } else {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        /// <summary>
        /// Finds an ancestor of an element, given a type.
        /// </summary>
        public static T FindAncestor<T>(this DependencyObject dependencyObject)
            where T : DependencyObject {
            return dependencyObject.FindAncestor<T>(null);
        }

        /// <summary>
        /// Finds an ancestor of an element, given the type and optionally a name.
        /// </summary>
        public static T FindAncestor<T>(
            this DependencyObject dependencyObject,
            string name
        ) where T : DependencyObject {
            T foundAncestor = null;
            DependencyObject parent = dependencyObject.GetVisualParent();
            while (parent != null) {
                T typedParent = parent as T;
                if (!string.IsNullOrEmpty(name)) {
                    FrameworkElement frameworkElement = parent as FrameworkElement;
                    if ((typedParent != null) && (
                            (frameworkElement == null) ||
                            (frameworkElement.Name == name)
                        )
                    ) {
                        foundAncestor = typedParent;
                        break;
                    }
                } else if (typedParent != null) {
                    foundAncestor = typedParent;
                    break;
                }
                parent = parent.GetVisualParent();
            }
            return foundAncestor;
        }

        /// <summary>
        /// Finds the direct visual parent of an element.
        /// </summary>
        public static DependencyObject GetVisualParent(this DependencyObject dependencyObject) {
            DependencyObject parentObject = null;
            if (dependencyObject != null) {
                ContentElement contentElement = dependencyObject as ContentElement;
                if (contentElement != null) {
                    DependencyObject parent = ContentOperations.GetParent(contentElement);
                    if (parent != null) {
                        parentObject = parent;
                    } else {
                        FrameworkContentElement frameworkContentElement = contentElement as FrameworkContentElement;
                        parentObject =
                            frameworkContentElement != null ? frameworkContentElement.Parent : null;
                    }
                } else {
                    parentObject = VisualTreeHelper.GetParent(dependencyObject);
                }
            }
            return parentObject;
        }

        //public static TV GetViewModel<TV, TU>(RoutedEventArgs e)
        //    where TV : ViewModelBase
        //    where TU : FrameworkElement {
        //    TV viewModel = null;
        //    DependencyObject dependencyObject = e.OriginalSource as DependencyObject;
        //    if (dependencyObject != null) {
        //        TU uiElement = dependencyObject.FindAncestor<TU>();
        //        if (uiElement != null) {
        //            viewModel = (TV)uiElement.DataContext;
        //        }
        //    }
        //    return viewModel;
        //}
    }
}
