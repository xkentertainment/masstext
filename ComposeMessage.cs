using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;

namespace MassText
{
    [Activity (Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = false, ScreenOrientation = ScreenOrientation.Portrait, NoHistory = true)]
    public class ComposeMessage : Activity
    {
        AutoCompleteTextView messageText;
        ListView listView;
        List<string> messages;
        Button sendButton;
        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            Xamarin.Essentials.Platform.Init (this, savedInstanceState);
            SetContentView (Resource.Layout.compose_message);
            contacts = StorageManager.GetCache<List<Contact>> (nameof (MainActivity));
            messages = new List<string> ();
            messageText = FindViewById (Resource.Id.messageField) as AutoCompleteTextView;
            messageText.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, contacts[0].possibleTitles);
            string defaultText = StorageManager.GetCache<string> ("defaultMessage");
            StorageManager.ClearCache ("defaultMessage");
            listView = FindViewById (Resource.Id.contactList) as ListView;
            listView.Adapter = new ArrayAdapter (this, Resource.Layout.abc_list_menu_item_layout, messages);

            messageText.AfterTextChanged += (sender, args) =>
            {
                ResolveMessages ();
                Task.Run (() =>
                {
                    if (messageText.SelectionEnd > 0)
                    {
                        if (messageText.Text[messageText.SelectionEnd - 1] == '{' || messageText.Text[messageText.SelectionEnd - 1] == '#')
                        {
                            RunOnUiThread (() =>
                            {
                                messageText.ShowDropDown ();
                            });
                        }
                        else
                        {
                            RunOnUiThread (() =>
                            {
                                messageText.DismissDropDown ();
                            });
                        }
                    }
                });
            };
            if (defaultText != null)
            {
                messageText.Text = defaultText;
            }

            messageText.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, contacts[0].possibleTitles);
            sendButton = FindViewById (Resource.Id.composeMessageSendBUtton) as Button;


            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                sendButton.Background.SetColorFilter (Color.Teal, PorterDuff.Mode.SrcAtop);
            }

            sendButton.Click += (sender, args) =>
            {
                if (messageText.Length () < 1)
                {
                    Snackbar.Make (Window.DecorView.RootView, "Message must contain at least one character", Snackbar.LengthLong).Show ();
                    return;
                }
                StorageManager.CacheObject (messages, nameof (ComposeMessage));
                StartActivity (typeof (SendMessages));
            };
        }
        List<Contact> contacts;
        void ResolveMessages ()
        {
            messages.Clear ();
            foreach (Contact contact in contacts)
            {
                string res = messageText.Text;
                foreach (string str in contact.possibleTitles)
                {
                    if (str.ToLower () != "mobile")
                    {
                        res = res.Replace ("{" + str + "}", contact.details[str].Trim ());
                        res = res.Replace ("#" + str, contact.details[str].Trim ());
                    }
                }
                messages.Add (res.Trim ());
            }
            listView.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, messages);
        }
    }
}