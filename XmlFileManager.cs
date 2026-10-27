using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Névjegyzék
{
    public static class XmlFileManager
    {
        public static readonly string ContactsFilePath = "contacts.xml";
        public static readonly string NotesFilePath = "notes.xml";
        public static readonly string GroupsFilePath = "groups.xml";

        public static List<Contact> ContactLoading()
        {
            List<Contact> contacts = new List<Contact>();
            if (File.Exists(ContactsFilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Contact>));
                using (StreamReader reader = new StreamReader(ContactsFilePath))
                {
                    contacts = (List<Contact>?)serializer.Deserialize(reader) ?? new List<Contact>();
                }
            }

            List<Note> notes = new List<Note>();
            if (File.Exists(NotesFilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Note>));
                using (StreamReader reader = new StreamReader(NotesFilePath))
                {
                    notes = (List<Note>?)serializer.Deserialize(reader) ?? new List<Note>();
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
            XmlSerializer serializer = new XmlSerializer(typeof(List<Contact>));
            using (StreamWriter writer = new StreamWriter(ContactsFilePath))
            {
                serializer.Serialize(writer, contacts);
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

            XmlSerializer notesSerializer = new XmlSerializer(typeof(List<Note>));
            using (StreamWriter writer = new StreamWriter(NotesFilePath))
            {
                notesSerializer.Serialize(writer, allNotes);
            }
        }

        public static List<ContactGroup> LoadGroups()
        {
            if (!File.Exists(GroupsFilePath))
            {
                return new List<ContactGroup>();
            }

            List<ContactGroup> groups = new List<ContactGroup>();
            XmlSerializer serializer = new XmlSerializer(typeof(List<ContactGroup>));
            using (StreamReader reader = new StreamReader(GroupsFilePath))
            {
                groups = (List<ContactGroup>?)serializer.Deserialize(reader) ?? new List<ContactGroup>();
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
            XmlSerializer serializer = new XmlSerializer(typeof(List<ContactGroup>));
            using (StreamWriter writer = new StreamWriter(GroupsFilePath))
            {
                serializer.Serialize(writer, groups);
            }
        }
    }
}