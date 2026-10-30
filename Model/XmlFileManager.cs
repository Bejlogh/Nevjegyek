using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace Névjegyzék
{
    public static class XmlFileManager
    {
        private static XmlDataModel DataModel;

        public static readonly string ContactsFilePath = "contacts.xml";
        public static readonly string NotesFilePath = "notes.xml";
        public static readonly string GroupsFilePath = "groups.xml";

        static XmlFileManager()
        {
            DataModel = new XmlDataModel(ContactsFilePath, NotesFilePath, GroupsFilePath);
        }

        public static XmlDataModel GetDataModel()
        {
            return DataModel ??= new XmlDataModel(ContactsFilePath, NotesFilePath, GroupsFilePath);
        }

        public static string GetProjectRootDirectory()
        {
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
        }

        public static string GetImagesDirectoryPath()
        {
            string imagesDir = Path.Combine(GetProjectRootDirectory(), "Images");
            Directory.CreateDirectory(imagesDir);
            return imagesDir;
        }

        private static void ShowXmlFileWarning(string fileName, Exception exception)
        {
            MessageBox.Show(
                $"A(z) '{fileName}' fájl sérült vagy hibás formátumú, ezért a program a hibás adatfájlt figyelmen kívül hagyja.\n\nRészletek: {exception.Message}",
                "Adatfájl hiba",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        public static List<Note> LoadNotes()
        {
            return DataModel.LoadNotes();
        }

        public static void SaveNotes(IEnumerable<Note> notes)
        {
            DataModel.SaveNotes(notes);
        }

        public static List<Contact> ContactLoading()
        {
            return DataModel.LoadContacts();
        }

        public static void ContactManaging(List<Contact> contacts)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Contact>));
                using (StreamWriter writer = new StreamWriter(ContactsFilePath))
                {
                    serializer.Serialize(writer, contacts);
                }
            }
            catch (Exception ex)
            {
                ShowXmlFileWarning(ContactsFilePath, ex);
                return;
            }

            var groups = LoadGroups();
            var allNotes = new Dictionary<string, Note>(StringComparer.OrdinalIgnoreCase);

            foreach (var contact in contacts)
            {
                contact.EnsureId();
                foreach (var note in contact.Notes)
                {
                    note.EnsureId();
                    note.ContactId = contact.Id;
                    note.ContactName = contact.Name;
                    note.GroupId = string.Empty;
                    allNotes[note.Id] = note;
                }
            }

            foreach (var group in groups)
            {
                group.EnsureId();
                foreach (var note in group.Notes)
                {
                    note.EnsureId();
                    note.GroupId = group.Id;
                    note.ContactId = string.Empty;
                    note.ContactName = string.Empty;
                    allNotes[note.Id] = note;
                }
            }

            SaveNotes(allNotes.Values);
        }

        private static void ReleaseImageLocks(string imagePath)
        {
            if (Application.Current == null)
            {
                return;
            }

            string normalizedTarget = Path.GetFullPath(imagePath);

            foreach (Window window in Application.Current.Windows.OfType<Window>())
            {
                foreach (Image image in FindImages(window))
                {
                    if (image.Source is BitmapImage bitmapImage)
                    {
                        var uri = bitmapImage.UriSource;
                        if (uri == null || string.Equals(Path.GetFullPath(uri.LocalPath), normalizedTarget, StringComparison.OrdinalIgnoreCase))
                        {
                            image.Source = null;
                        }
                    }
                    else if (image.Source != null)
                    {
                        image.Source = null;
                    }
                }
            }
        }

        public static void ClearAllImageSources()
        {
            if (Application.Current == null)
            {
                return;
            }

            foreach (Window window in Application.Current.Windows.OfType<Window>())
            {
                foreach (Image image in FindImages(window))
                {
                    image.Source = null;
                }
            }
        }

        private static IEnumerable<Image> FindImages(DependencyObject dependencyObject)
        {
            if (dependencyObject == null)
            {
                yield break;
            }

            if (dependencyObject is Image image)
            {
                yield return image;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
            {
                foreach (var child in FindImages(VisualTreeHelper.GetChild(dependencyObject, i)))
                {
                    yield return child;
                }
            }
        }

        public static void DeleteContactImage(Contact contact)
        {
            if (contact == null || string.IsNullOrWhiteSpace(contact.ImagePath))
            {
                return;
            }

            string imagePath = contact.ImagePath;
            if (!Path.IsPathRooted(imagePath))
            {
                string projectRoot = GetProjectRootDirectory();
                string rootRelativePath = imagePath.Replace('/', '\\');

                if (rootRelativePath.StartsWith("Images\\", StringComparison.OrdinalIgnoreCase) || rootRelativePath.Equals("Images", StringComparison.OrdinalIgnoreCase))
                {
                    imagePath = Path.Combine(projectRoot, rootRelativePath);
                }
                else
                {
                    imagePath = Path.Combine(projectRoot, imagePath);
                }
            }

            imagePath = Path.GetFullPath(imagePath);
            ReleaseImageLocks(imagePath);
            ClearAllImageSources();

            if (!File.Exists(imagePath))
            {
                return;
            }

            for (int attempt = 0; attempt < 30; attempt++)
            {
                try
                {
                    File.SetAttributes(imagePath, FileAttributes.Normal);
                    File.Delete(imagePath);
                    if (!File.Exists(imagePath))
                    {
                        return;
                    }
                }
                catch (IOException)
                {
                    if (attempt == 29)
                    {
                        break;
                    }

                    Thread.Sleep(250);
                }
                catch (UnauthorizedAccessException)
                {
                    if (attempt == 29)
                    {
                        break;
                    }

                    Thread.Sleep(250);
                }

                if (attempt == 14)
                {
                    ReleaseImageLocks(imagePath);
                    ClearAllImageSources();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            try
            {
                if (File.Exists(imagePath))
                {
                    File.SetAttributes(imagePath, FileAttributes.Normal);
                    File.Delete(imagePath);
                }
            }
            catch
            {

            }
        }

        public static List<ContactGroup> LoadGroups()
        {
            return DataModel.LoadGroups();
        }

        public static void SaveGroups(List<ContactGroup> groups)
        {
            DataModel.SaveGroups(groups);

            var contacts = ContactLoading();
            var allNotes = new Dictionary<string, Note>(StringComparer.OrdinalIgnoreCase);

            foreach (var contact in contacts)
            {
                foreach (var note in contact.Notes)
                {
                    note.EnsureId();
                    note.ContactId = contact.Id;
                    note.ContactName = contact.Name;
                    note.GroupId = string.Empty;
                    allNotes[note.Id] = note;
                }
            }

            foreach (var group in groups)
            {
                foreach (var note in group.Notes)
                {
                    note.EnsureId();
                    note.GroupId = group.Id;
                    note.ContactId = string.Empty;
                    note.ContactName = string.Empty;
                    allNotes[note.Id] = note;
                }
            }

            SaveNotes(allNotes.Values);
        }
    }
}