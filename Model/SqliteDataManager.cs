using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Névjegyzék
{
    public static class SqliteDataManager
    {
        public static string DatabaseFilePath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "nevjegyzek.db");

        public static void Rebuild(IEnumerable<Contact> contacts, IEnumerable<Note> notes)
        {
            var contactList = (contacts ?? Array.Empty<Contact>()).ToList();

            using var connection = new SqliteConnection($"Data Source={DatabaseFilePath}");
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                CreateSchema(connection, transaction);
                ClearTables(connection, transaction);

                foreach (var contact in contactList)
                {
                    InsertContact(connection, transaction, contact);
                }

                var contactIds = new HashSet<string>(
                    contactList.Select(contact => contact.Id),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var note in notes ?? Array.Empty<Note>())
                {
                    InsertNote(connection, transaction, note, contactIds);
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
            using var command = connection.CreateCommand();
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

                CREATE TABLE IF NOT EXISTS Notes (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Title TEXT,
                    Description TEXT,
                    ContactId TEXT,
                    ContactName TEXT,
                    FOREIGN KEY (ContactId) REFERENCES Contacts(Id) ON DELETE SET NULL
                );
                """;
            command.ExecuteNonQuery();
        }

        private static void ClearTables(SqliteConnection connection, SqliteTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Notes; DELETE FROM Residences; DELETE FROM Contacts;";
            command.ExecuteNonQuery();
        }

        private static void InsertContact(
            SqliteConnection connection,
            SqliteTransaction transaction,
            Contact contact)
        {
            contact.EnsureId();

            using var command = connection.CreateCommand();
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
        }

        private static void InsertNote(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Note note,
        ISet<string> contactIds)
        {
            note.EnsureId();

            string contactId = string.IsNullOrWhiteSpace(note.ContactId)
                || !contactIds.Contains(note.ContactId)
                ? null
                : note.ContactId;

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Notes (Id, Title, Description, ContactId, ContactName)
                VALUES ($id, $title, $description, $contactId, $contactName);
                """;
            command.Parameters.AddWithValue("$id", note.Id);
            command.Parameters.AddWithValue("$title", DbValue(note.Title));
            command.Parameters.AddWithValue("$description", DbValue(note.Description));
            command.Parameters.AddWithValue("$contactId", DbValue(contactId));
            command.Parameters.AddWithValue("$contactName", DbValue(note.ContactName));
            command.ExecuteNonQuery();
        }

        private static object DbValue(string value) => (object?)value ?? DBNull.Value;
    }
}