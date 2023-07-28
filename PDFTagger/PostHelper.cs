using System;
using System.IO;

namespace PDFTagger
{
    public static class PostHelper {

        public static bool GetPropertiesFromFilename(string filename, out string company, out DateTime date, out string description) {
            // e.g. "K:\DropBox\Post\Achmea 20161221 - Concept aanvraag medische expertise.pdf"
            string year;
            string month;
            string day;
            string companyAndDate;
            string dateString = null;
            company = null;
            date = DateTime.MinValue;
            description = null;

            try {
                var shortFilename = Path.GetFileNameWithoutExtension(filename);

                int i0 = shortFilename.IndexOf("-");
                if (i0 > -1) {
                    description = shortFilename.Substring(i0 + 1).Trim();
                    companyAndDate = shortFilename.Substring(0, i0).Trim();
                } else {
                    //
                    // If there is no '-' we assume the text to be "<Company> <Date>"
                    //
                    companyAndDate = shortFilename.Trim();
                }

                int i1 = companyAndDate.LastIndexOf(" ");
                if (i1 > -1) {
                    company = companyAndDate.Substring(0, i1).Trim();
                    dateString = companyAndDate.Substring(i1 + 1).Trim();
                } else {
                    //
                    // If there is no ' ' we assume the text to be "<Company>" or "<Date>"
                    //
                    if (companyAndDate[0] >= '0' || companyAndDate[0] <= '9') {
                        dateString = companyAndDate;
                    } else {
                        //
                        // We have a company - Description, or a NEW file
                        //
                        if(!string.IsNullOrWhiteSpace(description)) {
                            company = companyAndDate;
                        } else {
                            return false;
                        }                        
                    }
                }
                if (!string.IsNullOrWhiteSpace(dateString)) {
                    date = new DateTime(int.Parse(dateString.Substring(0, 4)), int.Parse(dateString.Substring(4, 2)), int.Parse(dateString.Substring(6, 2)));
                }
                return true;
            } catch(Exception) {
                return false;
            }
        }

        public static string GetShortFilenameWithExtensionFromProperties(string company, DateTime date, string description) {
            string filename = company;            
            if(date != DateTime.MinValue) {
                if (!string.IsNullOrWhiteSpace(filename)) {
                    filename += " ";
                }
                filename += $"{date:yyyyMMdd}";
            }
            if(!string.IsNullOrWhiteSpace(description)) {
                if (!string.IsNullOrWhiteSpace(filename)) {
                    filename += " - ";
                }
                filename += description;
            }
            if(!string.IsNullOrWhiteSpace(filename)) {
                filename += ".pdf";
            } else {
                filename = null;
            }
            return filename;
        }

        public static string[] GetPdfFilesFromDefaultFolder() {
            string defaultFolder = Directory.GetCurrentDirectory();
            return Directory.GetFiles(defaultFolder, "*.pdf", SearchOption.AllDirectories);
        }
    }
}
