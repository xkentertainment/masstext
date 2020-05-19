using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MassText
{
    [Activity (Label = "Logs", Theme = "@style/AppTheme", MainLauncher = false, ScreenOrientation = ScreenOrientation.Portrait, NoHistory = true)]
    public class Logs : Activity
    {
        ListView list;
        public bool AllowInput;
        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            SetContentView (Resource.Layout.logs);
            list = FindViewById (Resource.Id.logsList) as ListView;

            messages = StorageManager.GetCollection<IntendedMessages> (nameof (IntendedMessages));
            List<string> _messages = (from IntendedMessages mess in messages
                                      select $"{mess.intendedTime.ToString ("dd/MM hh:mm")}\n{mess.messages[0]}").ToList ();
            list.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, _messages);
            list.ItemClick += (Sender, args) =>
            {
                if (!AllowInput)
                    return;
                AllowInput = false;
                StorageManager.CacheObject (messages[(int)args.Id], nameof (ViewIntendedMessages));
                StartActivity (typeof (ViewIntendedMessages));
            };
            AllowInput = true;
        }
        List<IntendedMessages> messages;
    }
}
