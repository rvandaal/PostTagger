using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using PDFTagger.ViewModels;
using System.IO;

namespace PDFTagger
{
    public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged {
        FileSelectionWatcher fileSelectionWatcher;
        string firstSelectedFile;
        private string company;
        private string dateString;
        private DateTime date;
        private string description;

        public ObservableCollection<string> SelectedFiles { get; private set; }
        public ObservableCollection<string> PredefinedCompanies { get; private set; }
        public ObservableCollection<string> PredefinedDescriptions { get; private set; }

        public string HintCompany => "Vul bedrijf in";
        public string HintDate => "Vul dagtekening in";
        public string HintDescription=> "Vul korte beschrijving in";

        public bool ArePropertiesChangedBySystem { get; set; }

        public string Company {
            get { return company; }
            set {
                if (SetPropertyClass(value, ref company, () => Company)) {
                    OnDocumentPropertyChanged();
                }
            }
        }

        public string DateString {
            get { return dateString; }
            set {
                if (SetPropertyClass(value, ref dateString, () => DateString)) {
                    DateTime tmp;
                    if(DateTime.TryParse(value, out tmp)) {
                        Date = tmp;
                    }
                }
            }
        }

        public DateTime Date {
            get { return date; }
            set {
                if (SetProperty(value, ref date, () => Date)) {
                    DateString = Date.ToLongDateString();
                    OnDocumentPropertyChanged();
                }
            }
        }

        public string Description {
            get { return description; }
            set {
                if (SetPropertyClass(value, ref description, () => Description)) {
                    OnDocumentPropertyChanged();
                }
            }
        }

        private string newFilename;
        public string NewFilename {
            get { return newFilename; }
            set {
                if (SetPropertyClass(value, ref newFilename, () => NewFilename)) {
                    FireNotifyPropertyChanged(() => NewFilenameVisible);
                }
            }
        }

        public bool NewFilenameVisible {
            get { return !string.IsNullOrWhiteSpace(NewFilename); }
        }

        private DelegateCommandWithCanExecute saveCommand;
        public DelegateCommandWithCanExecute SaveCommand {
            get { return saveCommand; }
            set { SetPropertyClass(value, ref saveCommand, () => SaveCommand); }
        }


        public MainWindowViewModel() {
            SelectedFiles = new ObservableCollection<string>();
            fileSelectionWatcher = new FileSelectionWatcher();
            fileSelectionWatcher.Updated += OnFileSelectionChanged;

            ArePropertiesChangedBySystem = false;
            SaveCommand = new DelegateCommandWithCanExecute(OnSave, false);

            List<string> companies = new List<string>();
            List<string> descriptions = new List<string>();
            GetUsedCompaniesAndDescriptions(ref companies, ref descriptions);
            companies.Sort();
            descriptions.Sort();
            PredefinedCompanies = new ObservableCollection<string>(companies);
            PredefinedDescriptions = new ObservableCollection<string>(descriptions);
        }

        private void OnFileSelectionChanged(object sender, EventArgs e) {
            ArePropertiesChangedBySystem = true;
            SelectedFiles.Clear();
            foreach(var selectedFile in fileSelectionWatcher.Selected) {
                var dir = Path.GetDirectoryName(selectedFile);
                if (dir == Directory.GetCurrentDirectory() && Path.GetExtension(selectedFile).ToLower().EndsWith("pdf")) {
                    firstSelectedFile = selectedFile;
                    SelectedFiles.Add(selectedFile);
                    string selectedcompany;
                    DateTime selecteddate;
                    string selecteddescription;
                    if (
                        PostHelper.GetPropertiesFromFilename(
                            selectedFile,
                            out selectedcompany,
                            out selecteddate,
                            out selecteddescription
                        )
                    ) {
                        Company = selectedcompany;
                        Date = selecteddate;
                        Description = selecteddescription;
                    } else {
                        //
                        // File was not properly formatted OR it is new
                        //
                        Company = null;
                        Date = DateTime.Today;
                        Description = null;
                    }
                    // Only support single selection for now. Or take the first in the case of multiselection.
                    //break;
                }             
            }
            ArePropertiesChangedBySystem = false;
        }

        private void OnDocumentPropertyChanged() {
            if (!ArePropertiesChangedBySystem && !string.IsNullOrWhiteSpace(firstSelectedFile)) {
                var newTmpFilename = PostHelper.GetShortFilenameWithExtensionFromProperties(Company, Date, Description);
                if(string.IsNullOrWhiteSpace(newTmpFilename)) {
                    // All properties are still empty
                    return;
                }
                var oldShortFilenameWithExtension = Path.GetFileName(firstSelectedFile);
                var newFullFilename = firstSelectedFile.Replace(oldShortFilenameWithExtension, newTmpFilename);
                if (string.IsNullOrEmpty(newFullFilename)) {
                    return;
                }
                int i = 2;
                string replacedFilename = newFullFilename;
                while (File.Exists(replacedFilename)) {
                    replacedFilename = newFullFilename.Replace(
                        ".pdf", " (" + (i++) + ").pdf"
                    );
                }
                SaveCommand.CanExecuteValue = true;
                NewFilename = replacedFilename;                
            }
        }

        private void OnSave() {
            if(string.IsNullOrEmpty(firstSelectedFile) || string.IsNullOrWhiteSpace(NewFilename)) {
                throw new Exception("Cannot rename file");
            }
            if(!PredefinedCompanies.Contains(Company)) {
                PredefinedCompanies.Add(Company);
            }
            if (!PredefinedDescriptions.Contains(Description)) {
                PredefinedDescriptions.Add(Description);
            }
            File.Move(firstSelectedFile, NewFilename);
            firstSelectedFile = NewFilename;
            SaveCommand.CanExecuteValue = false;
            NewFilename = null;
            firstSelectedFile = null;
            Company = null;
            Date = DateTime.Today;
            Description = null;
        }

        private void GetUsedCompaniesAndDescriptions(ref List<string> companies, ref List<string> descriptions) {
            string company;
            DateTime date;
            string description;
            companies.Clear();
            descriptions.Clear();
            string[] files = PostHelper.GetPdfFilesFromDefaultFolder();
            foreach(var file in files) {
                if(PostHelper.GetPropertiesFromFilename(file, out company, out date, out description)) {
                    if(!string.IsNullOrWhiteSpace(company) && !companies.Contains(company)) {
                        companies.Add(company);
                    }
                    if (!string.IsNullOrWhiteSpace(description) && !descriptions.Contains(description)) {
                        descriptions.Add(description);
                    }
                }
            }
        }
    }
}
