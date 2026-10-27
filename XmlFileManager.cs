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
        public static readonly string ContactsFilePath = "contacts.xml";
        public static readonly string NotesFilePath = "notes.xml";
        public static readonly string GroupsFilePath = "groups.xml";

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

        public static List<Contact> ContactLoading()
        {
            List<Contact> contacts = new List<Contact>();
            if (File.Exists(ContactsFilePath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Contact>));
                    using (StreamReader reader = new StreamReader(ContactsFilePath))
                    {
                        contacts = (List<Contact>?)serializer.Deserialize(reader) ?? new List<Contact>();
                    }
                }
                catch (Exception ex)
                {
                    ShowXmlFileWarning(ContactsFilePath, ex);
                    return new List<Contact>();
                }
            }

            List<Note> notes = new List<Note>();
            if (File.Exists(NotesFilePath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Note>));
                    using (StreamReader reader = new StreamReader(NotesFilePath))
                    {
                        notes = (List<Note>?)serializer.Deserialize(reader) ?? new List<Note>();
                    }
                }
                catch (Exception ex)
                {
                    ShowXmlFileWarning(NotesFilePath, ex);
                    notes = new List<Note>();
                }
            }

            foreach (var contact in contacts)
            {
                contact.Notes.Clear();
            }

            foreach (var note in notes)
            {
                var contact = contacts.FirstOrDefault(c => c.Name == note.ContactName);
                if (contact != null)
                {
                    contact.Notes.Add(note);
                }
            }

            return contacts;
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

            List<Note> allNotes = new List<Note>();
            foreach (var contact in contacts)
            {
                foreach (var note in contact.Notes)
                {
                    note.ContactName = contact.Name;
                    allNotes.Add(note);
                }
            }

            try
            {
                XmlSerializer notesSerializer = new XmlSerializer(typeof(List<Note>));
                using (StreamWriter writer = new StreamWriter(NotesFilePath))
                {
                    notesSerializer.Serialize(writer, allNotes);
                }
            }
            catch (Exception ex)
            {
                ShowXmlFileWarning(NotesFilePath, ex);
            }
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
                //wpf kép lock miatt probalkozhat a user tovabb
            }
        }

        public static List<ContactGroup> LoadGroups()
        {
            if (!File.Exists(GroupsFilePath))
            {
                return new List<ContactGroup>();
            }

            List<ContactGroup> groups = new List<ContactGroup>();
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ContactGroup>));
                using (StreamReader reader = new StreamReader(GroupsFilePath))
                {
                    groups = (List<ContactGroup>?)serializer.Deserialize(reader) ?? new List<ContactGroup>();
                }
            }
            catch (Exception ex)
            {
                ShowXmlFileWarning(GroupsFilePath, ex);
                return new List<ContactGroup>();
            }

            var globalContacts = ContactLoading();
            foreach (var group in groups)
            {
                for (int i = 0; i < group.Contacts.Count; i++)
                {
                    var groupContact = group.Contacts[i];
                    var freshContact = globalContacts.FirstOrDefault(c => c.Name == groupContact.Name && c.Phone == groupContact.Phone);
                    if (freshContact != null)
                    {
                        group.Contacts[i] = freshContact;
                    }
                }
            }

            return groups;
        }

        public static void SaveGroups(List<ContactGroup> groups)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ContactGroup>));
                using (StreamWriter writer = new StreamWriter(GroupsFilePath))
                {
                    serializer.Serialize(writer, groups);
                }
            }
            catch (Exception ex)
            {
                ShowXmlFileWarning(GroupsFilePath, ex);
            }
        }
    }
}