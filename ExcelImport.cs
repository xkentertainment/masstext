using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
using Aspose.Cells;

namespace MassText
{
    public static class ExcelImport
    {
        public static List<Contact> Process (Stream stream)
        {
            Workbook workbook = new Workbook (stream);
            var sheet = workbook.Worksheets[0];

            List<Contact> contacts = new List<Contact> ();

            List<string> titles = new List<string> ();
            string workBookName = workbook.FileName;
            StorageManager.CacheObject (workBookName, nameof (workBookName));
            if (sheet.Cells["A1"].StringValue == "Message")
            {
                StorageManager.CacheObject<string> (sheet.Cells["B1"].StringValue, "defaultMessage");

                for (int i = 1; i <= sheet.Cells.MaxDataColumn; i++)
                {
                    titles.Add (sheet.Cells[1, i].StringValue);
                }

                for (int r = 2; r <= sheet.Cells.MaxDataRow; r++)
                {
                    contacts.Add (new Contact (titles));
                    for (int c = 1; c <= titles.Count; c++)
                    {
                        contacts[^1].details.Add (titles[c - 1], sheet.Cells[r, c].StringValue);
                    }
                }
            }
            else
            {
                //Get titles
                for (int i = 0; i <= sheet.Cells.MaxDataColumn; i++)
                {
                    titles.Add (sheet.Cells[0, i].StringValue);
                }

                for (int r = 1; r <= sheet.Cells.MaxDataRow; r++)
                {
                    contacts.Add (new Contact (titles));
                    for (int c = 0; c < titles.Count; c++)
                    {
                        contacts[^1].details.Add (titles[c], sheet.Cells[r, c].StringValue);
                    }
                }
            }
            return contacts;
        }
    }
    [Serializable]
    public class Contact
    {
        public List<string> possibleTitles;
        public Dictionary<string, string> details;
        public Contact (List<string> possibleTitles)
        {
            details = new Dictionary<string, string> ();
            this.possibleTitles = possibleTitles;
        }
    }
    [Serializable]
    public class IntendedMessages
    {
        public readonly string id;
        public readonly List<Contact> contacts;
        public readonly List<string> messages;
        public readonly List<MessageStatus> statuses;
        public readonly DateTime intendedTime;
        public readonly int delayTime;
        public readonly TimeSpan repeatTime;
        public readonly string phoneKey;
        public readonly string name;
        public readonly bool repeat;

        public IntendedMessages (string id, string name, List<Contact> contacts, List<string> messages, DateTime intendedTime, TimeSpan repeatTime, int delayTime, string phoneKey, bool repeat = false)
        {
            this.name = name;
            this.id = id;
            this.contacts = contacts;
            this.messages = messages;
            this.intendedTime = intendedTime;
            this.repeatTime = repeatTime;
            statuses = new List<MessageStatus> ();
            foreach (Contact _ in contacts) statuses.Add (MessageStatus.Pending);
            this.delayTime = delayTime;
            this.phoneKey = phoneKey;
            this.repeat = repeat;
        }
    }
    public enum MessageStatus
    {
        Pending,
        Sent,
        Delivered,
        Failed,
        Periodic
    }

}
