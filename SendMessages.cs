using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Telephony;
using System.Threading.Tasks;
using Android.App.Job;
using Android.Support.Design.Widget;
using AndroidX.Work;
using Android.Graphics;

namespace MassText
{
    [Activity (Label = "SendMessages", NoHistory = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class SendMessages : Activity
    {
        Button sendNowButton;
        TextView previewMessage;
        Button scheduleButton;
        View scheduleMessageForm;
        Spinner delayTypeSpinner;
        TextView delayTimeInput;
        TextView timeOfDayInput;
        Button discard;
        Button sendBySchedule;
        CheckBox repeatCheckBox;
        TextView timeOfDayHeader;
        TextView messageBetweenDelay;
        TextView dateHeader;
        TextView dateInput;
        Spinner delayBetweenTypeSpinner;
        string workBookName;
        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);
            // Create your application here
            SetContentView (Resource.Layout.send_message);

            contacts = StorageManager.GetCache<List<Contact>> (nameof (MainActivity));
            messages = StorageManager.GetCache<List<string>> (nameof (ComposeMessage));
            workBookName = StorageManager.GetCache<string> (nameof (workBookName));
            int phoneColumn = StorageManager.GetCache<int> (MainActivity.phoneNumberColumnID);
            phoneKey = contacts[0].details.Keys.ToArray ()[phoneColumn];


            sendNowButton = FindViewById (Resource.Id.sendNowButton) as Button;
            sendNowButton.Click += SendNow;

            previewMessage = FindViewById (Resource.Id.messagePreview) as TextView;
            previewMessage.Text = messages[0];

            scheduleButton = FindViewById (Resource.Id.scheduleMessageButton) as Button;
            scheduleMessageForm = FindViewById (Resource.Id.messageSchedulerForm);

            scheduleButton.Click += (sender, args) =>
              {
                  scheduleMessageForm.Visibility = scheduleMessageForm.Visibility == ViewStates.Gone ? ViewStates.Visible : ViewStates.Gone;
              };
            delayTypeSpinner = FindViewById (Resource.Id.delayTypeSpinner) as Spinner;
            delayTypeSpinner.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, new List<string> () { "Minutes", "Hours", "Days", "Weeks", "Months" });

            dateInput = FindViewById (Resource.Id.dateInput) as TextView;
            dateHeader = FindViewById (Resource.Id.dateInputHeader) as TextView;

            delayTypeSpinner.ItemSelected += (sender, args) =>
              {
                  if (delayTypeSpinner.SelectedItemId > 1)
                  {
                      timeOfDayInput.Visibility = ViewStates.Visible;
                      timeOfDayHeader.Visibility = ViewStates.Visible;

                      if (delayTypeSpinner.SelectedItemId == 4)
                      {
                          dateInput.Visibility = ViewStates.Visible;
                          dateHeader.Visibility = ViewStates.Visible;
                      }
                  } 
                  else
                  {
                      timeOfDayInput.Text = string.Empty;
                      timeOfDayInput.Visibility = ViewStates.Gone;
                      timeOfDayHeader.Visibility = ViewStates.Gone;
                      dateInput.Visibility = ViewStates.Gone;
                      dateHeader.Visibility = ViewStates.Gone;
                  }
              };
            discard = FindViewById (Resource.Id.discardMessageButton) as Button;
            discard.Click += (sender, args) =>
            {
                OnBackPressed ();
            };
            delayTimeInput = FindViewById (Resource.Id.delayTimeInput) as TextView;
            timeOfDayInput = FindViewById (Resource.Id.timeOfDayInput) as TextView;
            timeOfDayHeader = FindViewById (Resource.Id.timeOfDayInputHeader) as TextView;

            sendBySchedule = FindViewById (Resource.Id.setScheduleButton) as Button;

            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                sendNowButton.Background.SetColorFilter (Color.Black, PorterDuff.Mode.SrcAtop);
                discard.Background.SetColorFilter (Color.DarkRed, PorterDuff.Mode.SrcAtop);
                scheduleButton.Background.SetColorFilter (Color.Teal, PorterDuff.Mode.SrcAtop);
                sendBySchedule.Background.SetColorFilter (Color.White, PorterDuff.Mode.SrcAtop);
            }

            sendBySchedule.Click += SendBySchedule;

            messageBetweenDelay = FindViewById (Resource.Id.messageBetweenDelay) as TextView;
            repeatCheckBox = FindViewById (Resource.Id.repeatCheckBox) as CheckBox;
            delayBetweenTypeSpinner = FindViewById (Resource.Id.delayBetweenTypeSpinner) as Spinner;
            delayBetweenTypeSpinner.Adapter = new ArrayAdapter (this, Resource.Layout.XMLFile1, new List<string> () { "Seconds", "Minutes" });

            allowInput = true;
        }

        private async void SendBySchedule (object sender, EventArgs e)
        {
            if(!allowInput)
            {
                return;
            }
            allowInput = false;
            void ToastError(string text)
            {
                RunOnUiThread (() =>
                {
                    Toast.MakeText (this, text, ToastLength.Long).Show ();
                    allowInput = true;
                });
            }
            int betweenDelay = 0;
            bool hasTimeOfDay = timeOfDayInput.Visibility == ViewStates.Visible;
            long initialDelay = 0;
            ulong timeDelay = 0;
            await Task.Run (() =>
            {
                try
                {
                    betweenDelay = int.Parse (messageBetweenDelay.Text);
                    switch(delayBetweenTypeSpinner.SelectedItemId)
                    {
                        case 0:
                            betweenDelay *= 1000;
                            break;
                        case 1:
                            betweenDelay *= 60000;
                            break;
                    }
                }
                catch
                {
                    ToastError ("Please only enter numbers in the message per hour field");
                    return;
                }
                try
                {
                    timeDelay = ulong.Parse (delayTimeInput.Text);
                }
                catch
                {
                    ToastError ("Invalid delay time");
                    return;
                }
                if (timeDelay < 1)
                {
                    ToastError ("Invalid delay time");
                    return;
                }
                if (hasTimeOfDay)
                {
                    DateTime targetTime;
                    try
                    {
                        targetTime = DateTime.ParseExact (timeOfDayInput.Text, "hh:mm:ss", new System.Globalization.CultureInfo ("en-US"));
                    }
                    catch
                    {
                        ToastError ("Date must be in the format of hh:mm:ss and 24 hour");
                        return;
                    }
                    if (targetTime < DateTime.Now)
                    {
                        targetTime.AddDays ((DateTime.Now - targetTime).Days);
                    }

                    initialDelay = (targetTime - DateTime.Now).Milliseconds;

                    try
                    {
                        int dayOfMonth = DateTime.Now.Add (TimeSpan.FromMilliseconds (initialDelay)).Day;
                        DateTime monthDate = DateTime.Now.Add (TimeSpan.FromMilliseconds (initialDelay));
                        initialDelay += (long)((monthDate.AddDays (-dayOfMonth).AddDays (int.Parse (dateInput.Text)) - DateTime.Now).TotalMilliseconds);
                    }
                    catch
                    {
                        ToastError ("invalid day of month");
                        return;
                    }
                }
            });
            await Task.Run (() =>
            {
                RunOnUiThread (() =>
                {
                    SetContentView (Resource.Layout.loading);
                });
            });
            await Task.Run (() =>
            {
                allowInput = false;
                //var jobBuilder = JobSchedulerHelpers.CreateJobBuilderUsingJobId<ScheduledMessagingJob> (this, IdGen.ServiceId ());
                //var scheduler = (JobScheduler)GetSystemService (JobSchedulerService);

                //jobBuilder.SetPersisted (true);
                //jobBuilder.SetBackoffCriteria (10000, BackoffPolicy.Linear);

                ulong period = 6000;
                switch (delayTypeSpinner.SelectedItemId)
                {
                    case 0:
                        period = 60000;
                        break;
                    case 1:
                        period = 3600000;
                        break;
                    case 2:
                        period = 86400000;
                        break;
                    case 3:
                        period = 604800000;
                        break;
                    case 4:
                        period = 2628000000;
                        break;
                }
                period *= timeDelay;

                period = Math.Clamp (period, 10000, period);
                if (!hasTimeOfDay)
                {
                    initialDelay = (long)period;
                }

                IntendedMessages intended = new IntendedMessages (IdGen.Generate (), workBookName, contacts, messages, DateTime.Now, TimeSpan.FromMilliseconds (period), betweenDelay, phoneKey, repeat: repeatCheckBox.Checked);
                StorageManager.AddToCollection (intended, nameof (IntendedMessages));

                Data targetData = new Data.Builder ().PutString ("_id", intended.id).Build ();
                OneTimeWorkRequest request = OneTimeWorkRequest.Builder.From<MessageServiceWorker> ()
                .SetInitialDelay (initialDelay, Java.Util.Concurrent.TimeUnit.Milliseconds)
                .SetInputData (targetData)
                .AddTag (intended.id)
                .Build ();
                WorkManager.Instance.Enqueue (request);
                Android.Util.Log.Debug ("MessageServiceWorker", $"Enqued Message {TimeSpan.FromMilliseconds (period).TotalMinutes} minutes from now");

                //var jobParams = new PersistableBundle ();
                //jobParams.PutString ("_id", intended.id);
                //jobBuilder.SetExtras (jobParams);

                //var jobInfo = jobBuilder.Build ();

                //var scheduleResult = scheduler.Schedule (jobInfo);


                //if (JobScheduler.ResultFailure == scheduleResult)
                //{
                //    Toast.MakeText (this, "Failed to schedule", ToastLength.Long).Show ();
                //}

                //Intent intent = new Intent (AlarmStateReciever.setOff);
                //intent.PutExtra ("_id", intended.id);
                //Intent intent = SendMessagesForegroundIntentCompat (this, intended.id);

                //PendingIntent pend = PendingIntent.GetForegroundService (this, 0, intent, 0);
                //var alarmManager = (AlarmManager)GetSystemService (AlarmService);
                //alarmManager.Set (AlarmType.ElapsedRealtimeWakeup, SystemClock.ElapsedRealtime () + period, pend);
            });

            RunOnUiThread (() =>
            {
                OnBackPressed ();
            });
        }

        List<Contact> contacts;
        List<string> messages;
        string phoneKey;

        bool allowInput = false;
        private async void SendNow (object sender, EventArgs e)
        {
            if (!allowInput)
                return;
            allowInput = false;
            int delay = 0;
            string target = string.Empty;
            await Task.Run (() =>
            {
                try
                {
                    delay = int.Parse (FindViewById<TextView> (Resource.Id.messageBetweenDelay).Text);
                }
                catch
                {
                    delay = 0;
                }
                RunOnUiThread (() =>
                {
                    SetContentView (Resource.Layout.loading);
                });
                IntendedMessages intended = new IntendedMessages (IdGen.Generate (),workBookName, contacts, messages, DateTime.Now, TimeSpan.Zero, delay, phoneKey);
                StorageManager.AddToCollection (intended, nameof (IntendedMessages));
                target = intended.id;
            });
            await Task.Run (() =>
            {
                SendMessagesNowCompat (this, target);
            });
            OnBackPressed ();
        }
        /// <summary>
        /// Starts the messaging service in the foreground
        /// </summary>
        /// <param name="context"></param>
        /// <param name="target"></param>
        public static void SendMessagesNowCompat (Context context, string target, bool checkPending = true, bool checkFailed = true)
        {
            Intent intent = SendMessagesForegroundIntentCompat (context, target, checkPending, checkFailed);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService (intent);
            }
            else
            {
                context.StartService (intent);
            }
        }
        public static Intent SendMessagesForegroundIntentCompat (Context context, string target, bool checkPending = true, bool checkFailed = true)
        {
            Intent intent = new Intent (context, typeof (MessengerService));
            intent.PutExtra (MessengerService.foreground, true);
            intent.PutExtra (MessengerService.toSend, target);
            intent.PutExtra ("_checkPending", checkPending);
            intent.PutExtra ("_checkFailed", checkFailed);
            return intent;
        }
    }
}