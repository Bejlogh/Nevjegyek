using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Névjegyzék
{
    public static class SqliteDataManager
    {
        public static string DatabaseFilePath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "nevjegyzek.db");

        public static bool DatabaseExists => File.Exists(DatabaseFilePath);

        public static bool HasGroupSchema
        {
            get
            {
                if (!DatabaseExists)
                {
                    return false;
                }

                using SqliteConnection connection = new SqliteConnection($"Data Source={DatabaseFilePath}");
                connection.Open();
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('Groups', 'GroupContacts', 'GroupNotes');";
                return Convert.ToInt32(command.ExecuteScalar()) == 3;
            }
        }

        public static void Rebuild(
            IEnumerable<Contact> contacts,
            IEnumerable<Note> notes,
            IEnumerable<ContactGroup> groups = null)
        {
            List<Contact> contactList = (contacts ?? Array.Empty<Contact>()).ToList();
            List<Note> noteList = (notes ?? Array.Empty<Note>()).ToList();
            List<ContactGroup> groupList = (groups ?? Array.Empty<ContactGroup>()).ToList();

            using SqliteConnection connection = new SqliteConnection($"Data Source={DatabaseFilePath}");
            connection.Open();

            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                CreateSchema(connection, transaction);
                ClearTables(connection, transaction);

                foreach (Contact contact in contactList)
                {
                    InsertContact(connection, transaction, contact);
                }

                HashSet<string> contactIds = new HashSet<string>(
                    contactList.Select(contact => contact.Id),
                    StringComparer.OrdinalIgnoreCase);

                foreach (Note note in noteList)
                {
                    note.EnsureId();
                }

                HashSet<string> noteIds = new HashSet<string>(
                    noteList.Select(note => note.Id),
                    StringComparer.OrdinalIgnoreCase);

                foreach (Note note in noteList)
                {
                    InsertNote(connection, transaction, note, contactIds);
                }

                foreach (ContactGroup group in groupList)
                {
                    InsertGroup(connection, transaction, group, contactIds, noteIds);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static void CreateSchema(SqliteConnection connection, SqliteTransaction transaction)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS Contacts (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Name TEXT,
                    Email TEXT,
                    Phone TEXT,
                    Position TEXT,
                    BirthDate TEXT,
                    Website TEXT,
                    ImagePath TEXT,
                    IsBlocked INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Residences (
                    ContactId TEXT NOT NULL PRIMARY KEY,
                    Address TEXT,
                    FOREIGN KEY (ContactId) REFERENCES Contacts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Attachments (
                    ContactId TEXT NOT NULL PRIMARY KEY,
                    FileName TEXT NOT NULL,
                    ContentType TEXT NOT NULL,
                    Data BLOB NOT NULL,
                    FOREIGN KEY (ContactId) REFERENCES Contacts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Notes (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Title TEXT,
                    Description TEXT,
                    ContactId TEXT,
                    FOREIGN KEY (ContactId) REFERENCES Contacts(Id) ON DELETE SET NULL
                );

                CREATE TABLE IF NOT EXISTS Groups (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Name TEXT
                );

                CREATE TABLE IF NOT EXISTS GroupContacts (
                    GroupId TEXT NOT NULL,
                    ContactId TEXT NOT NULL,
                    PRIMARY KEY (GroupId, ContactId),
                    FOREIGN KEY (GroupId) REFERENCES Groups(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ContactId) REFERENCES Contacts(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS GroupNotes (
                    GroupId TEXT NOT NULL,
                    NoteId TEXT NOT NULL,
                    PRIMARY KEY (GroupId, NoteId),
                    FOREIGN KEY (GroupId) REFERENCES Groups(Id) ON DELETE CASCADE,
                    FOREIGN KEY (NoteId) REFERENCES Notes(Id) ON DELETE CASCADE
                );
                """;
            command.ExecuteNonQuery();
        }

        private static void ClearTables(SqliteConnection connection, SqliteTransaction transaction)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM GroupNotes; DELETE FROM GroupContacts; DELETE FROM Groups; DELETE FROM Notes; DELETE FROM Attachments; DELETE FROM Residences; DELETE FROM Contacts;";
            command.ExecuteNonQuery();
        }

        private static void InsertContact(
            SqliteConnection connection,
            SqliteTransaction transaction,
            Contact contact)
        {
            contact.EnsureId();

            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Contacts
                    (Id, Name, Email, Phone, Position, BirthDate, Website, ImagePath, IsBlocked)
                VALUES
                    ($id, $name, $email, $phone, $position, $birthDate, $website, $imagePath, $isBlocked);

                INSERT INTO Residences (ContactId, Address)
                VALUES ($id, $address);
                """;
            command.Parameters.AddWithValue("$id", contact.Id);
            command.Parameters.AddWithValue("$name", DbValue(contact.Name));
            command.Parameters.AddWithValue("$email", DbValue(contact.Email));
            command.Parameters.AddWithValue("$phone", DbValue(contact.Phone));
            command.Parameters.AddWithValue("$position", DbValue(contact.Position));
            command.Parameters.AddWithValue("$birthDate", DbValue(contact.BirthDate));
            command.Parameters.AddWithValue("$website", DbValue(contact.Website));
            command.Parameters.AddWithValue("$imagePath", DbValue(contact.ImagePath));
            command.Parameters.AddWithValue("$isBlocked", contact.IsBlocked ? 1 : 0);
            command.Parameters.AddWithValue("$address", DbValue(contact.Address));
            command.ExecuteNonQuery();

            if (!string.IsNullOrWhiteSpace(contact.ImagePath) && File.Exists(contact.ImagePath))
            {
                string fileName = Path.GetFileName(contact.ImagePath);
                string contentType = GetImageContentType(Path.GetExtension(fileName));
                using SqliteCommand attachmentCommand = connection.CreateCommand();
                attachmentCommand.Transaction = transaction;
                attachmentCommand.CommandText = "INSERT INTO Attachments (ContactId, FileName, ContentType, Data) VALUES ($contactId, $fileName, $contentType, $data);";
                attachmentCommand.Parameters.AddWithValue("$contactId", contact.Id);
                attachmentCommand.Parameters.AddWithValue("$fileName", fileName);
                attachmentCommand.Parameters.AddWithValue("$contentType", contentType);
                attachmentCommand.Parameters.Add("$data", SqliteType.Blob).Value = File.ReadAllBytes(contact.ImagePath);
                attachmentCommand.ExecuteNonQuery();
            }
        }

        private static string GetImageContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }

        private static void InsertNote(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Note note,
        ISet<string> contactIds)
        {
            note.EnsureId();

            string? contactId = string.IsNullOrWhiteSpace(note.GroupId)
                && !string.IsNullOrWhiteSpace(note.ContactId)
                && contactIds.Contains(note.ContactId)
                ? note.ContactId
                : null;

            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Notes (Id, Title, Description, ContactId)
                VALUES ($id, $title, $description, $contactId);
                """;
            command.Parameters.AddWithValue("$id", note.Id);
            command.Parameters.AddWithValue("$title", DbValue(note.Title));
            command.Parameters.AddWithValue("$description", DbValue(note.Description));
            command.Parameters.AddWithValue("$contactId", DbValue(contactId));
            command.ExecuteNonQuery();
        }

        private static void InsertGroup(
            SqliteConnection connection,
            SqliteTransaction transaction,
            ContactGroup group,
            ISet<string> contactIds,
            ISet<string> noteIds)
        {
            group.EnsureId();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO Groups (Id, Name) VALUES ($id, $name);";
                command.Parameters.AddWithValue("$id", group.Id);
                command.Parameters.AddWithValue("$name", DbValue(group.Name));
                command.ExecuteNonQuery();
            }

            foreach (Contact contact in group.Contacts)
            {
                contact.EnsureId();
                if (!contactIds.Contains(contact.Id))
                {
                    continue;
                }

                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO GroupContacts (GroupId, ContactId) VALUES ($groupId, $contactId);";
                command.Parameters.AddWithValue("$groupId", group.Id);
                command.Parameters.AddWithValue("$contactId", contact.Id);
                command.ExecuteNonQuery();
            }

            foreach (Note note in group.Notes)
            {
                note.EnsureId();
                if (!noteIds.Contains(note.Id))
                {
                    continue;
                }

                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO GroupNotes (GroupId, NoteId) VALUES ($groupId, $noteId);";
                command.Parameters.AddWithValue("$groupId", group.Id);
                command.Parameters.AddWithValue("$noteId", note.Id);
                command.ExecuteNonQuery();
            }
        }

        public static (List<Contact> Contacts, List<Note> Notes, List<ContactGroup> Groups) LoadSnapshot()
        {
            using SqliteConnection connection = new SqliteConnection($"Data Source={DatabaseFilePath}");
            connection.Open();

            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                CreateSchema(connection, transaction);
                transaction.Commit();
            }

            List<Contact> contacts = new List<Contact>();
            Dictionary<string, Contact> contactsById = new Dictionary<string, Contact>(StringComparer.OrdinalIgnoreCase);
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT Contacts.Id, Contacts.Name, Contacts.Email, Contacts.Phone,
                           Contacts.Position, Contacts.BirthDate, Contacts.Website,
                           Contacts.ImagePath, Contacts.IsBlocked, Residences.Address
                    FROM Contacts
                    LEFT JOIN Residences ON Residences.ContactId = Contacts.Id;
                    """;
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Contact contact = new Contact
                    {
                        Id = reader.GetString(0),
                        Name = ReadString(reader, 1),
                        Email = ReadString(reader, 2),
                        Phone = ReadString(reader, 3),
                        Position = ReadString(reader, 4),
                        BirthDate = ReadString(reader, 5),
                        Website = ReadString(reader, 6),
                        ImagePath = ReadString(reader, 7),
                        IsBlocked = reader.GetInt64(8) != 0,
                        Address = ReadString(reader, 9)
                    };
                    contacts.Add(contact);
                    contactsById.Add(contact.Id, contact);
                }
            }

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT ContactId, FileName, Data FROM Attachments;";
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (contactsById.TryGetValue(reader.GetString(0), out Contact contact))
                    {
                        string extension = Path.GetExtension(ReadString(reader, 1)).ToLowerInvariant();
                        if (extension is not (".jpg" or ".jpeg" or ".png" or ".bmp"))
                        {
                            throw new InvalidDataException($"Unsupported profile image extension '{extension}'.");
                        }

                        string fileName = $"{Guid.NewGuid():N}{extension}";
                        string imagePath = Path.Combine(XmlFileManager.GetImagesDirectoryPath(), fileName);
                        File.WriteAllBytes(imagePath, reader.GetFieldValue<byte[]>(2));
                        contact.ImagePath = imagePath;
                    }
                }
            }

            List<Note> notes = new List<Note>();
            Dictionary<string, Note> notesById = new Dictionary<string, Note>(StringComparer.OrdinalIgnoreCase);
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id, Title, Description, ContactId FROM Notes;";
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Note note = new Note
                    {
                        Id = reader.GetString(0),
                        Title = ReadString(reader, 1),
                        Description = ReadString(reader, 2),
                        ContactId = ReadString(reader, 3)
                    };
                    if (!string.IsNullOrWhiteSpace(note.ContactId)
                        && contactsById.TryGetValue(note.ContactId, out Contact contact))
                    {
                        note.ContactName = contact.Name;
                        contact.Notes.Add(note);
                    }
                    else
                    {
                        note.ContactId = string.Empty;
                    }

                    notes.Add(note);
                    notesById.Add(note.Id, note);
                }
            }

            List<ContactGroup> groups = new List<ContactGroup>();
            Dictionary<string, ContactGroup> groupsById = new Dictionary<string, ContactGroup>(StringComparer.OrdinalIgnoreCase);
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id, Name FROM Groups;";
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ContactGroup group = new ContactGroup
                    {
                        Id = reader.GetString(0),
                        Name = ReadString(reader, 1)
                    };
                    groups.Add(group);
                    groupsById.Add(group.Id, group);
                }
            }

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT GroupId, ContactId FROM GroupContacts;";
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (groupsById.TryGetValue(reader.GetString(0), out ContactGroup group)
                        && contactsById.TryGetValue(reader.GetString(1), out Contact contact))
                    {
                        group.Contacts.Add(contact);
                    }
                }
            }

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT GroupId, NoteId FROM GroupNotes;";
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (groupsById.TryGetValue(reader.GetString(0), out ContactGroup group)
                        && notesById.TryGetValue(reader.GetString(1), out Note note))
                    {
                        note.GroupId = group.Id;
                        note.ContactId = string.Empty;
                        note.ContactName = string.Empty;
                        group.Notes.Add(note);
                    }
                }
            }

            return (contacts, notes, groups);
        }

        private static string ReadString(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static object DbValue(string value) => (object?)value ?? DBNull.Value;
    }
}