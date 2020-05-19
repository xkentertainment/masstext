using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Content.PM;
using System.Threading.Tasks;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Views;
using Android.Provider;
using Android.Net;
using Android.Database;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.Content;

namespace MassText
{
    [Activity (Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {
        Button openExcelButton;
        Button getFromContacts;
        Button openLogsButton;
        ProgressBar progressBar;
        protected override void OnCreate (Bundle savedInstanceState)
        {
            Xamarin.Essentials.Platform.Init (Application);
            base.OnCreate (savedInstanceState);

            inContactSelection = false;
            StorageManager.ClearCache (nameof (MainActivity));
            StorageManager.ClearCache (nameof (ComposeMessage));

            //Init ();
        }
        void Init ()
        {
            if (loading)
            {
                loading = false;
                return;
            }

            SetContentView (Resource.Layout.activity_main);

            openExcelButton = FindViewById (Resource.Id.importExcelFromDeviceButton) as Button;

            openExcelButton.Click += (sender, args) => { InitImport (false); };

            progressBar = FindViewById (Resource.Id.importProgressBar) as ProgressBar;
            openLogsButton = FindViewById (Resource.Id.viewLogsButton) as Button;
            openLogsButton.Click += (sender, args) =>
            {
                StartActivity (typeof (Logs));
            };

            getFromContacts = FindViewById (Resource.Id.importContactFromDeviceButton) as Button;
            getFromContacts.Click += (sender, args) =>
            {
                InitImport (true);
            };
            PowerManager man = (GetSystemService (PowerService) as PowerManager);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && !man.IsIgnoringBatteryOptimizations (PackageName))
            {
                Snackbar.Make (openExcelButton.Parent as View, "Please allow ignoring of battery optimization in order to use proper scheduling", Snackbar.LengthIndefinite)
                    .SetAction ("OK",
                    (args) =>
                    {
                        Android.Content.Intent intent = new Intent ();
                        intent.SetAction (Settings.ActionRequestIgnoreBatteryOptimizations);
                        intent.SetData (Android.Net.Uri.Parse ("package:" + PackageName));
                        StartActivityForResult (intent, 104);
                    })
                    .Show ();
            }

            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                openExcelButton.Background.SetColorFilter (Resources.GetColor (Resource.Color.material_deep_teal_500), PorterDuff.Mode.SrcAtop);
                getFromContacts.Background.SetColorFilter (Color.White, PorterDuff.Mode.SrcAtop);
                openLogsButton.Background.SetColorFilter (Color.Black, PorterDuff.Mode.SrcAtop);
            }
        }
        const int getContentRequest = 101;
        const int requestFileSystemPerms = 102;
        const int getContactRequest = 103;
        private async void InitImport (bool contacts)
        {
            await Task.Run (() =>
            {
                RunOnUiThread (() =>
                {
                    openExcelButton.Enabled = false;
                    getFromContacts.Enabled = false;
                    openLogsButton.Enabled = false;
                    progressBar.Visibility = ViewStates.Visible;
                });
            });
            void Start ()
            {
                if (!contacts)
                {
                    Android.Content.Intent intent = new Android.Content.Intent (Android.Content.Intent.ActionGetContent);
                    string[] types = { "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
                    intent.SetType ("*/*");
                    intent.PutExtra (Android.Content.Intent.ExtraMimeTypes, types);
                    StartActivityForResult (intent, getContentRequest);
                }
                else
                {
                    SetContentView (Resource.Layout.loading);
                    Task.Run (() =>
                    {
                        QueryContacts ();
                        RunOnUiThread (() =>
                        {
                            if (queriedContacts.Count > 0)
                            {
                                EnterContactSelection ();
                            }
                            else
                            {
                                if (queriedContacts.Count > 0)
                                {
                                    Snackbar.Make (openExcelButton.Parent as View, "No Contacts found", Snackbar.LengthLong).Show ();
                                }
                            }
                        });
                    });
                }
            }
            bool hasPerms () => (Android.Support.V4.Content.ContextCompat.CheckSelfPermission (this, Android.Manifest.Permission.ReadExternalStorage) == (int)Permission.Granted
             && Android.Support.V4.Content.ContextCompat.CheckSelfPermission (this, Android.Manifest.Permission.SendSms) == (int)Permission.Granted
             && Android.Support.V4.Content.ContextCompat.CheckSelfPermission (this, Android.Manifest.Permission.ReceiveSms) == (int)Permission.Granted
             && Android.Support.V4.Content.ContextCompat.CheckSelfPermission (this, Android.Manifest.Permission.ReadContacts) == (int)Permission.Granted
             && Android.Support.V4.Content.ContextCompat.CheckSelfPermission (this, Android.Manifest.Permission.WriteSms) == (int)Permission.Granted);
            if (hasPerms ())
            {
                Start ();
            }
            else
            {
                await Task.Run (() =>
                {
                    RunOnUiThread (() =>
                    {
                        Snackbar.Make (openExcelButton.Parent as View, "Press OK, then grant permissions to proceed", Snackbar.LengthIndefinite)
                                .SetAction ("OK", (view) =>
                                {
                                    ActivityCompat.RequestPermissions (this, new string[] { Android.Manifest.Permission.ReadExternalStorage, Android.Manifest.Permission.WriteExternalStorage, Android.Manifest.Permission.SendSms, Android.Manifest.Permission.WriteSms, Android.Manifest.Permission.ReceiveSms, Android.Manifest.Permission.ReadContacts }, requestFileSystemPerms);
                                    requestingPerms = true;

                                })
                                .Show ();
                        requestingPerms = true;
                    });
                });
                await Task.Run (() => { while (requestingPerms) { } });

                if (hasPerms ())
                {
                    Start ();
                }
                else
                {
                    Snackbar.Make (openExcelButton.Parent as View, "You cannot use this app's functions without granting all permissions", Snackbar.LengthLong).Show ();
                }
            }

            await Task.Run (() =>
            {
                try
                {
                    RunOnUiThread (() =>
                    {
                        openExcelButton.Enabled = true;
                        getFromContacts.Enabled = true;
                        openLogsButton.Enabled = true;
                        progressBar.Visibility = ViewStates.Visible;
                    });
                }
                catch
                {

                }
            });
        }
        bool inContactSelection;
        protected override void OnResume ()
        {
            base.OnResume ();
            Init ();
        }
        public override void OnBackPressed ()
        {
            if (inContactSelection)
            {
                Init ();
            }
            else
            {
                base.OnBackPressed ();
            }
        }
        TextView searchBox;
        ListView contactList;
        Button proceedButton;
        List<bool> selectedContacts;
        List<string> contactDisplays;
        ArrayAdapter contactAdapter;
        Button selectAllButton;
        void EnterContactSelection ()
        {
            inContactSelection = true;
            SetContentView (Resource.Layout.contact_picker);
            searchBox = FindViewById (Resource.Id.contactListSearch) as TextView;
            contactList = FindViewById (Resource.Id.contactSelectionList) as ListView;
            proceedButton = FindViewById (Resource.Id.proceedButton) as Button;
            selectAllButton = FindViewById (Resource.Id.selectAllButton) as Button;

            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                proceedButton.Background.SetColorFilter (Resources.GetColor (Resource.Color.material_deep_teal_200), PorterDuff.Mode.SrcAtop);
                selectAllButton.Background.SetColorFilter (Color.White, PorterDuff.Mode.SrcAtop);
            }
            contactDisplays = new List<string> ();
            SetContactDisplays ((queriedContacts, null));
            selectedContacts = (from string _ in contactDisplays select false).ToList ();
            contactList.Adapter = contactAdapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, contactDisplays);
            contactList.ItemClick += (sender, args) =>
              {
                  (bool success, int index) = IndexFromNameAndNumber (contactDisplays[args.Position]);
                  if (success)
                  {
                      selectedContacts[index] = !selectedContacts[index];
                      (List<Contact>, List<bool>) results = SearchContactSelection (searchBox.Text);
                      SetContactDisplays ((results.Item1,results.Item2));
                      contactList.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, contactDisplays);
                  }
              };
            searchBox.AfterTextChanged += (sender, args) =>
            {
                string text = searchBox.Text.Replace ("\\", string.Empty);
                (List<Contact>, List<bool>) results = SearchContactSelection (text);
                SetContactDisplays ((results.Item1, results.Item2));
                contactList.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, contactDisplays);
            };
            proceedButton.Click += (sender, args) =>
            {
                List<Contact> approved = new List<Contact> ();
                for (int i = 0; i < selectedContacts.Count; i++)
                {
                    if (selectedContacts[i] == true)
                    {
                        approved.Add (queriedContacts[i]);
                    }
                }
                if (approved.Count < 1)
                {
                    Toast.MakeText (this, "Please select at least 1 contact", ToastLength.Long).Show ();
                    return;
                }
                StorageManager.CacheObject (1, phoneNumberColumnID);
                StorageManager.CacheObject (approved, nameof (MainActivity));
                StartActivity (new Intent (this, typeof (ComposeMessage)));
            };
            selectAllButton.Click += (sender, args) =>
            {
                foreach (string contactDisplay in contactDisplays)
                {
                    if (contactDisplays.Count == queriedContacts.Count)
                    {
                        selectedContacts[IndexFromNameAndNumber (contactDisplay).Item2] = !selectedContacts[IndexFromNameAndNumber (contactDisplay).Item2];
                    }
                    else
                    {
                        selectedContacts[IndexFromNameAndNumber (contactDisplay).Item2] = true;
                    }
                }
                string text = searchBox.Text.Replace ("\\", string.Empty);
                (List<Contact>, List<bool>) results = SearchContactSelection (text);
                SetContactDisplays ((results.Item1, results.Item2));
                contactList.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, contactDisplays);
            };
        }
        (bool, int) IndexFromNameAndNumber (string display)
        {
            string[] values = System.Text.RegularExpressions.Regex.Split (display, "[\n]");

            for (int i = 0; i < queriedContacts.Count; i++)
            {
                if (queriedContacts[i].details["name"] == values[0] && queriedContacts[i].details["phone"] == values[1])
                    return (true, i);
            }
            return (false, -1);
        }
        void SetContactDisplays((List<Contact> of,List<bool> selected) @params)
        {
            contactDisplays.Clear ();
            string selected = "\n(Selected)";
            for (int i = 0; i < @params.of.Count; i++)
            {
                Contact contact = @params.of[i];
                string stat = (@params.selected != null && @params.selected[i]) ? selected : string.Empty;
                contactDisplays.Add ($"{contact.details["name"]}\n{contact.details["phone"]}{stat}");
            }
        }
        bool Match (string name,string searchParam)
        {
            if (searchParam.Length == 0 || name.Length == 0)
                return true;
            if (searchParam.Length < 3 && searchParam.Length == 1)
            {
                return name[0].ToString ().ToLower () == searchParam[0].ToString ().ToLower ();
            }
            else
            {
                return name.ToLower ().Contains (searchParam.ToLower ());
            }
        }
        (List<Contact>, List<bool>) SearchContactSelection (string searchParam)
        {
            List<Contact> results = new List<Contact> ();
            List<bool> toggles = new List<bool> ();
            for (int i = 0; i < queriedContacts.Count; i++)
            {
                Contact contact = queriedContacts[i];
                if (Match (contact.details["name"], searchParam))
                {
                    results.Add (contact);
                    toggles.Add (selectedContacts[i]);
                }
            }
            return (results, toggles);
        }

        List<Contact> queriedContacts = new List<Contact> ();
        void QueryContacts()
        {
            ContentResolver cr = ContentResolver;
            ICursor cur = cr.Query (ContactsContract.Contacts.ContentUri, null, null, null, null);

            if ((cur != null ? cur.Count : 0) > 0)
            {
                while (cur != null && cur.MoveToNext ())
                {
                    string id = cur.GetString (cur.GetColumnIndex (ContactsContract.Contacts.InterfaceConsts.Id));
                    string name = cur.GetString (cur.GetColumnIndex (ContactsContract.Contacts.InterfaceConsts.DisplayName));
                    if (cur.GetInt (cur.GetColumnIndex (
                            ContactsContract.Contacts.InterfaceConsts.HasPhoneNumber)) > 0)
                    {
                        ICursor pCur = cr.Query (
                                ContactsContract.CommonDataKinds.Phone.ContentUri,
                                null,
                                ContactsContract.CommonDataKinds.Phone.InterfaceConsts.ContactId + " = ?",
                                new string[] { id }, null);

                        while (pCur.MoveToNext ())
                        {
                            string phoneNo = pCur.GetString (pCur.GetColumnIndex (ContactsContract.CommonDataKinds.Phone.Number));

                            queriedContacts.Add (new Contact (new List<string> () { "name" })
                            {
                                details = new Dictionary<string, string> ()
                                        {
                                            { "name", name },
                                            { "phone", PhoneNumberProcessor.Cleaned(phoneNo) }
                                        }
                            });
                        }
                        pCur.Close ();
                    }
                }
            }
            if (cur != null)
            {
                cur.Close ();
            }
            if (queriedContacts.Count > 1)
            {
                FilterQueriedContacts ();
            }
        }
        void FilterQueriedContacts()
        {
            List<Contact> filteredContacts = new List<Contact> ();
            List<string> numberPool = new List<string> ();
            foreach (Contact contact in queriedContacts)
            {
                if (!numberPool.Contains (contact.details["phone"]) && contact.details["phone"] != string.Empty)
                {
                    filteredContacts.Add (contact);
                    numberPool.Add (contact.details["phone"]);
                }
            }
            queriedContacts = filteredContacts;
        }
        bool loading;
        protected override void OnActivityResult (int requestCode, [GeneratedEnum] Result resultCode, Android.Content.Intent data)
        {
            base.OnActivityResult (requestCode, resultCode, data);
            if (requestCode == getContentRequest)
            {
                if (resultCode == Result.Ok)
                {
                    loading = true;
                    SetContentView (Resource.Layout.loading);
                    CreateContactList (data);
                }
                else
                {
                    openExcelButton.Enabled = true;
                    progressBar.Visibility = ViewStates.Gone;
                }
            }
            if (requestCode == getContactRequest)
            {
                if (resultCode == Result.Ok)
                {
                    SetContentView (Resource.Layout.loading);
                }
                else
                {
                    openExcelButton.Enabled = true;
                    progressBar.Visibility = ViewStates.Gone;
                }
            }
        }
        public static readonly string phoneNumberColumnID = "Phone COlumn";
        async void CreateContactListFromContacts ()
        {
            await Task.Run (() =>
            {
            });
        }
        async void CreateContactList (Android.Content.Intent data)
        {
            await Task.Run (() =>
            {
                try
                {
                    Stream stream = ContentResolver.OpenInputStream (data.Data);
                    byte[] buffer = new byte[32768];
                    int read;
                    Stream output = new MemoryStream ();

                    while ((read = stream.Read (buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write (buffer, 0, read);
                    }

                    List<Contact> contacts = ExcelImport.Process (output);
                    if (contacts.Count > 0)
                    {
                        StorageManager.CacheObject (contacts, nameof (MainActivity));
                        string[] keys = contacts[0].details.Keys.ToArray ();
                        for (int i = 0; i < keys.Length; i++)
                        {
                            if (keys[i].ToLower () == "mobile"
                            || keys[i].ToLower () == "phonenumber"
                            || keys[i].ToLower () == "phone number"
                            || keys[i].ToLower () == "number"
                            || keys[i].ToLower () == "contact"
                            || keys[i].ToLower () == "simu"
                            || keys[i].ToLower () == "namba ya simu"
                            || keys[i].ToLower () == "denwa bango"
                            || keys[i].ToLower () == "telephone")
                            {
                                StorageManager.CacheObject (i, phoneNumberColumnID);
                                break;
                            }
                        }
                    }

                    RunOnUiThread (() =>
                    {
                        if (contacts.Count > 0)
                        {
                            StartActivity (typeof (ComposeMessage));
                        }
                        else
                        {
                            SetContentView (Resource.Layout.activity_main);
                            Init ();
                            Snackbar.Make (Window.DecorView.RootView, "Could not obtain any contact information from this spreadsheet", Snackbar.LengthLong).Show ();
                        }
                    });
                }
                catch (Exception exp)
                {
                    RunOnUiThread (() =>
                    {
                        if (exp is OutOfMemoryException)
                        {
                            Snackbar.Make (Window.DecorView.RootView, "Out of Memory Error has occured, Please close some apps then try again", Snackbar.LengthLong).Show ();
                        }
                        else
                        {
                            Snackbar.Make (Window.DecorView.RootView, "An unexpected error has occured importing this file", Snackbar.LengthLong)
                                    .SetAction ("OK", (args) => { StartActivity (new Android.Content.Intent (this, typeof (MainActivity))); Finish (); })
                                    .Show ();
                        }
                    });
                }

            });
        }
        bool requestingPerms;
        public override void OnRequestPermissionsResult (int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult (requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult (requestCode, permissions, grantResults);
            requestingPerms = false;
        }
    }
}