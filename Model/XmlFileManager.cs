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
                $"{string.Format(LanguageManager.GetString("Dialogs_XmlFileCorrupt"), fileName)}\n\n{LanguageManager.GetString("Dialogs_DetailsLabel")} {exception.Message}",
                LanguageManager.GetString("Dialogs_XmlFileErrorTitle"),
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
            try
            {
                SynchronizeToSqlite();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LanguageManager.GetString("Dialogs_XmlSavedSqliteSyncFailed")}\n\n{LanguageManager.GetString("Dialogs_DetailsLabel")} {ex.Message}",
                    LanguageManager.GetString("Dialogs_SqliteSyncErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        public static void SynchronizeToSqlite()
        {
            List<Contact> contacts = DataModel.LoadContacts();
            List<Note> notes = DataModel.LoadNotes();
            List<ContactGroup> groups = DataModel.LoadGroups();
            SqliteDataManager.Rebuild(contacts, notes, groups);
        }

        public static void SynchronizeFromSqlite()
        {
            List<ContactGroup>? legacyGroups = SqliteDataManager.HasGroupSchema
                ? null
                : DataModel.LoadGroups();
            (List<Contact> Contacts, List<Note> Notes, List<ContactGroup> Groups) snapshot = SqliteDataManager.LoadSnapshot();
            List<ContactGroup> groups = snapshot.Groups;

            if (legacyGroups != null)
            {
                Dictionary<string, Contact> contactsById = snapshot.Contacts.ToDictionary(contact => contact.Id, StringComparer.OrdinalIgnoreCase);
                foreach (ContactGroup group in legacyGroups)
                {
                    group.Contacts = new System.Collections.ObjectModel.ObservableCollection<Contact>(
                        group.Contacts
                            .Select(contact => contactsById.TryGetValue(contact.Id, out Contact updatedContact)
                                ? updatedContact
                                : snapshot.Contacts.FirstOrDefault(candidate =>
                                    candidate.Name == contact.Name && candidate.Phone == contact.Phone))
                            .Where(contact => contact != null));

                    group.Notes = new System.Collections.ObjectModel.ObservableCollection<Note>(
                        group.Notes
                            .Select(groupNote => snapshot.Notes.FirstOrDefault(note =>
                                string.Equals(note.Id, groupNote.Id, StringComparison.OrdinalIgnoreCase)))
                            .Where(note => note != null));

                    foreach (Note note in group.Notes)
                    {
                        note.GroupId = group.Id;
                        note.ContactId = string.Empty;
                        note.ContactName = string.Empty;
                    }
                }

                SqliteDataManager.Rebuild(snapshot.Contacts, snapshot.Notes, legacyGroups);
                groups = legacyGroups;
            }

            DataModel.SaveContacts(snapshot.Contacts);
            DataModel.SaveGroups(groups);
            DataModel.SaveNotes(snapshot.Notes);
        }

        public static List<Contact> ContactLoading()
        {
            return DataModel.LoadContacts();
        }

        public static void SaveContactsAndNotes(List<Contact> contacts, IEnumerable<Note> allNotes)
        {
            foreach (Contact contact in contacts)
            {
                contact.EnsureId();
                foreach (Note note in contact.Notes)
                {
                    note.EnsureId();
                    note.ContactId = contact.Id;
                    note.ContactName = contact.Name;
                    note.GroupId = string.Empty;
                }
            }

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

            Dictionary<string, Note> normalizedNotes = new Dictionary<string, Note>(StringComparer.OrdinalIgnoreCase);

            foreach (Note note in allNotes ?? Enumerable.Empty<Note>())
            {
                note.EnsureId();
                normalizedNotes[note.Id] = note;
            }

            foreach (Note note in normalizedNotes.Values)
            {
                bool assignedToContact = contacts.Any(contact =>
                    contact.Notes.Any(contactNote => contactNote.Id == note.Id));
                bool assignedToGroup = LoadGroups().Any(group =>
                    group.Notes.Any(groupNote => groupNote.Id == note.Id));

                if (!assignedToContact && !assignedToGroup)
                {
                    note.ContactId = string.Empty;
                    note.ContactName = string.Empty;
                    note.GroupId = string.Empty;
                }
            }

            HashSet<string> contactAssignedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Contact contact in contacts)
            {
                foreach (Note note in contact.Notes)
                {
                    note.ContactId = contact.Id;
                    note.ContactName = contact.Name;
                    note.GroupId = string.Empty;
                    normalizedNotes[note.Id] = note;
                    contactAssignedIds.Add(note.Id);
                }
            }

            foreach (ContactGroup group in LoadGroups())
            {
                foreach (Note note in group.Notes)
                {
                    note.EnsureId();
                    if (!contactAssignedIds.Contains(note.Id) && normalizedNotes.ContainsKey(note.Id))
                    {
                        note.GroupId = group.Id;
                        note.ContactId = string.Empty;
                        note.ContactName = string.Empty;
                        normalizedNotes[note.Id] = note;
                    }
                }
            }

            SaveNotes(normalizedNotes.Values);
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

            List<ContactGroup> groups = LoadGroups();
            Dictionary<string, Note> allNotes = new Dictionary<string, Note>(StringComparer.OrdinalIgnoreCase);

            foreach (Contact contact in contacts)
            {
                contact.EnsureId();
                foreach (Note note in contact.Notes)
                {
                    note.EnsureId();
                    note.ContactId = contact.Id;
                    note.ContactName = contact.Name;
                    note.GroupId = string.Empty;
                    allNotes[note.Id] = note;
                }
            }

            foreach (ContactGroup group in groups)
            {
                group.EnsureId();
                foreach (Note note in group.Notes)
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
                        Uri? uri = bitmapImage.UriSource;
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
                foreach (Image child in FindImages(VisualTreeHelper.GetChild(dependencyObject, i)))
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

            List<Contact> contacts = ContactLoading();
            Dictionary<string, Note> allNotes = new Dictionary<string, Note>(StringComparer.OrdinalIgnoreCase);

            foreach (Contact contact in contacts)
            {
                foreach (Note note in contact.Notes)
                {
                    note.EnsureId();
                    note.ContactId = contact.Id;
                    note.ContactName = contact.Name;
                    note.GroupId = string.Empty;
                    allNotes[note.Id] = note;
                }
            }

            foreach (ContactGroup group in groups)
            {
                foreach (Note note in group.Notes)
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