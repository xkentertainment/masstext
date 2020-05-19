using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Work;

namespace MassText
{
    [Activity (Label = "ViewIntendedMessages",ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait,NoHistory =true)]
    public class ViewIntendedMessages : Activity
    {
        Spinner filter;
        ListView list;
        Button retryFailed;
        Button forcePending;
        Button delete;
        TextView fileName;
        TextView bundleStatus;
        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            SetContentView (Resource.Layout.view_messages);
            messages = StorageManager.GetCache<IntendedMessages> (nameof (ViewIntendedMessages));

            filter = FindViewById (Resource.Id.filterSpinner) as Spinner;
            retryFailed = FindViewById (Resource.Id.resendFailedBUtton) as Button;
            forcePending = FindViewById (Resource.Id.forceSendPendingButton) as Button;
            delete = FindViewById (Resource.Id.deleteButton) as Button;

            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                retryFailed.Background.SetColorFilter (Color.Black, PorterDuff.Mode.SrcAtop);
                forcePending.Background.SetColorFilter (Color.White, PorterDuff.Mode.SrcAtop);
                delete.Background.SetColorFilter (Color.DarkRed, PorterDuff.Mode.SrcAtop);
            }

            list = FindViewById (Resource.Id.viewMessagesList) as ListView;
            fileName = FindViewById (Resource.Id.fileName) as TextView;
            bundleStatus = FindViewById (Resource.Id.statusOfMessageBundle) as TextView;
            fileName.Text = StorageManager.GetCache<string> ("workBookName");

            filter.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, new List<string> () { all, failed, sent, delivered, pending });

            filter.ItemSelected += (sender, args) =>
              {
                  switch (args.Id)
                  {
                      case 1:
                          list.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, GetMessages (MessageStatus.Failed));
                          break;
                      case 2:
                          list.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, GetMessages (MessageStatus.Sent));
                          break;
                      case 3:
                          list.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, GetMessages (MessageStatus.Delivered));
                          break;
                      case 4:
                          list.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, GetMessages (MessageStatus.Pending));
                          break;
                      case 0:
                          list.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, GetMessages ());
                          break;
                  }
              };
            fails = (from MessageStatus status in messages.statuses
                     where status != MessageStatus.Delivered || status != MessageStatus.Sent
                     select status).Count ();

            bundleStatus.Text = messages.repeatTime != TimeSpan.Zero ? (messages.repeat ? $"Scheduled Every {messages.repeatTime}" : (DateTime.Now > messages.intendedTime.Add (messages.repeatTime) ? ((fails > 0 ? "Scheduled, Sent with errors" : "Sent")) : "Scheduled")) : (fails > 0 ? "Sent with errors" : "Sent");
            bundleStatus.Text += $"{(messages.repeat ? "\nRepeating" : "")}";
            retryFailed.Click += (sender, args) =>
              {
                  if (fails > 0)
                  {
                      SendMessages.SendMessagesNowCompat (this, messages.id, true, false);
                  }
                  else
                  {
                      Toast.MakeText (this, "All messages already sent", ToastLength.Long).Show ();
                  }
              }; 
            forcePending.Click += (sender, args) =>
              {
                  if (fails > 0)
                  {
                      SendMessages.SendMessagesNowCompat (this, messages.id, false, true);
                  }
                  else
                  {
                      Toast.MakeText (this, "All messages already sent", ToastLength.Long).Show ();
                  }
              };
            delete.Click += (sender, args) =>
            {
                List<IntendedMessages> intendeds = StorageManager.GetCollection<IntendedMessages> (nameof (IntendedMessages));
                List<IntendedMessages> idMatches = (from IntendedMessages mess in intendeds
                                                    where mess.id == messages.id && mess.intendedTime==messages.intendedTime
                                                    select mess).ToList ();
                intendeds.Remove (idMatches[0]);
                StorageManager.CacheObject (intendeds, nameof (IntendedMessages));
                WorkManager.Instance.CancelAllWorkByTag(idMatches[0].id);
                Android.Util.Log.Debug ("MessengerServiceWorker", $"Work {idMatches[0].id} cancelled");
                OnBackPressed ();
            };
        }
        int fails;
        List<string> GetMessages (MessageStatus status)
        {
            List<string> result = new List<string> ();
            for (int i = 0; i < messages.statuses.Count; i++)
            {
                if (messages.statuses[i] == status)
                {
                    result.Add (messages.messages[i]);
                }
            }
            return result;
        }
        List<string> GetMessages ()
        {
            List<string> result = new List<string> ();
            for (int i = 0; i < messages.statuses.Count; i++)
            {
                result.Add ($"{messages.statuses[i]}\n{messages.messages[i]}\n{messages.contacts[i].details[messages.phoneKey]}");
            }
            return result;
        }
        const string failed = "Failed";
        const string pending = "Pending";
        const string sent = "Sent";
        const string delivered = "Delivered";
        const string all = "All";
        IntendedMessages messages;
    }
}