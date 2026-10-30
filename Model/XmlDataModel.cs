using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace Névjegyzék
{
    public class XmlDataModel
    {
        public string ContactsFilePath { get; }
        public string NotesFilePath { get; }
        public string GroupsFilePath { get; }

        public XmlDataModel(
            string contactsFilePath = "contacts.xml",
            string notesFilePath = "notes.xml",
            string groupsFilePath = "groups.xml")
        {
            ContactsFilePath = contactsFilePath;
            NotesFilePath = notesFilePath;
            GroupsFilePath = groupsFilePath;
        }

        private static void ShowXmlFileWarning(string fileName, Exception exception)
        {
            MessageBox.Show(
                $"A(z) '{fileName}' fájl sérült vagy hibás formátumú, ezért a program a hibás adatfájlt figyelmen kívül hagyja.\n\nRészletek: {exception.Message}",
                "Adatfájl hiba",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        public List<Note> LoadNotes()
        {
            if (!File.Exists(NotesFilePath))
            {
                return new List<Note>();
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Note>));
                using (StreamReader reader = new StreamReader(NotesFilePath))
                {
                    return (List<Note>?)serializer.Deserialize(reader) ?? new List<Note>();
                }
            }
            catch (Exception ex)
            {
                ShowXmlFileWarning(NotesFilePath, ex);
                return new List<Note>();
            }
        }

        public void SaveNotes(IEnumerable<Note> notes)
        {
            var normalizedNotes = notes
                .Where(n => n != null)
                .GroupBy(n => n.Id)
                .Select(g => g.First())
                .ToList();

            foreach (var note in normalizedNotes)
            {
                note.EnsureId();
            }

            try
            {
                XmlSerializer notesSerializer = new XmlSerializer(typeof(List<Note>));
                using (StreamWriter writer = new StreamWriter(NotesFilePath))
                {
                    notesSerializer.Serialize(writer, normalizedNotes);
                }
            }
            catch (Exception ex)
            {
                ShowXmlFileWarning(NotesFilePath, ex);
            }
        }

        public List<Contact> LoadContacts()
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

            List<Note> notes = LoadNotes();

            foreach (var contact in contacts)
            {
                contact.EnsureId();
                contact.Notes.Clear();
            }

            foreach (var note in notes)
            {
                note.EnsureId();
                var contact = !string.IsNullOrWhiteSpace(note.ContactId)
                    ? contacts.FirstOrDefault(c => c.Id == note.ContactId)
                    : contacts.FirstOrDefault(c => c.Name == note.ContactName);
                if (contact != null)
                {
                    note.ContactId = contact.Id;
                    note.ContactName = contact.Name;
                    contact.Notes.Add(note);
                }
            }

            return contacts;
        }

        public void SaveContacts(IEnumerable<Contact> contacts)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Contact>));
                using (StreamWriter writer = new StreamWriter(ContactsFilePath))
                {
                    serializer.Serialize(writer, contacts.ToList());
                }
            }
            catch (Exception ex)
            {
                ShowXmlFileWarning(ContactsFilePath, ex);
                return;
            }
        }

        public List<ContactGroup> LoadGroups()
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

            var globalContacts = LoadContacts();
            var notes = LoadNotes();
            foreach (var group in groups)
            {
                group.EnsureId();
                group.Notes.Clear();
                for (int i = 0; i < group.Contacts.Count; i++)
                {
                    var groupContact = group.Contacts[i];
                    if (string.IsNullOrWhiteSpace(groupContact.Id))
                    {
                        groupContact.EnsureId();
                    }

                    var freshContact = globalContacts.FirstOrDefault(c => c.Id == groupContact.Id)
                        ?? globalContacts.FirstOrDefault(c => c.Name == groupContact.Name && c.Phone == groupContact.Phone);
                    if (freshContact != null)
                    {
                        group.Contacts[i] = freshContact;
                    }
                }

                foreach (var note in notes.Where(n => n.GroupId == group.Id))
                {
                    note.EnsureId();
                    note.GroupId = group.Id;
                    group.Notes.Add(note);
                }
            }

            return groups;
        }

        public void SaveGroups(List<ContactGroup> groups)
        {
            foreach (var group in groups)
            {
                group.EnsureId();
                foreach (var contact in group.Contacts)
                {
                    contact.EnsureId();
                }
                foreach (var note in group.Notes)
                {
                    note.EnsureId();
                    note.GroupId = group.Id;
                    note.ContactId = string.Empty;
                    note.ContactName = string.Empty;
                }
            }

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
