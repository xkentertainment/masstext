using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using Android.Views;
using Android.Widget;
using AndroidX.Work;

namespace MassText
{
    [Service (Name = "com.xk.masstextapp.messenger", Permission = "android.permission.FOREGROUND_SERVICE")]
    class MessengerService : Service
    {

        public const int serviceRunningId = 0x12ff6f;

        public override IBinder OnBind (Intent intent)
        {
            return null;
        }
        public static readonly string toSend = "TO_SEND";
        public static readonly string foreground = "_fore";
        public const string sent = "MASS_SMS_SENT";
        public const string delivered = "MASS_SMS_DELIVERED";
        List<string> workingIds;
        public override void OnCreate ()
        {
            base.OnCreate ();
        }
        public override void OnDestroy ()
        {
            base.OnDestroy ();
            StorageManager.CacheObject (new List<string> (), nameof (workingIds));
        }
        public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
        {
            NotificationChannel channel = new NotificationChannel ("massText.Service.Notif", "MassText", NotificationImportance.Max);
            (GetSystemService (NotificationService) as NotificationManager).CreateNotificationChannel (channel);
            if (intent.GetBooleanExtra (foreground, false))
            {
                var notification = new Notification.Builder (this, channel.Id)
                    .SetContentTitle (Resources.GetString (Resource.String.app_name))
                    .SetContentText ("MassText is Running")
                    .SetSmallIcon (Resource.Drawable.abc_btn_check_material)
                    .SetOngoing (true)
                    .Build ();

                StartForeground (serviceRunningId, notification);
            }
            string targetId = intent.GetStringExtra (toSend);
            bool checkPending = intent.GetBooleanExtra ("_checkPending", true);
            bool checkFailed = intent.GetBooleanExtra ("_checkFailed", true);
            workingIds = StorageManager.GetCollection<string> (nameof (workingIds));

            SendMessages (this, targetId, checkPending, checkFailed, StopSelf);
            return StartCommandResult.Sticky;
        }

        public static void SendMessages (Context context, string targetId, bool checkPending, bool checkFailed, Action callback = null, Action<IntendedMessages> repeatcheck = null, bool async = true)
        {
            List<IntendedMessages> intendedMessages = StorageManager.GetCollection<IntendedMessages> (nameof (IntendedMessages));
            List<IntendedMessages> filtered = (from IntendedMessages intd in intendedMessages
                                               where intd.id == targetId
                                               select intd).ToList ();

            int target = intendedMessages.IndexOf (filtered[0]);
            bool CanSend (MessageStatus status)
            {
                if (status == MessageStatus.Periodic)
                {
                    return true;
                }

                bool resultC = false;
                bool resultP = false;
                if (checkPending)
                {
                    resultC = status == MessageStatus.Pending;
                }
                else
                {
                    resultC = true;
                }
                if (checkFailed)
                {
                    resultP = status == MessageStatus.Failed;
                }
                else
                {
                    resultP = true;
                }
                return resultC || resultP;
            }
            if (target >= 0 && target < intendedMessages.Count && intendedMessages[target].intendedTime < DateTime.Now)
            {
                repeatcheck?.Invoke (intendedMessages[target]);
                SmsManager manager = SmsManager.Default;
                List<string> messages = intendedMessages[target].messages;
                List<Contact> contacts = intendedMessages[target].contacts;
                string phoneKey = intendedMessages[target].phoneKey;
                int delay = intendedMessages[target].delayTime;
                List<MessageStatus> statuses = intendedMessages[target].statuses;
                void DoWork ()
                {
                    for (int i = 0; i < messages.Count && i < contacts.Count; i++)
                    {

                        if (CanSend (statuses[i]))
                        {
                            Data messData = new Data.Builder ()
                                .PutString ("_message", messages[i])
                                .PutString ("_id", intendedMessages[target].id)
                                .PutInt ("_index", i)
                                .PutString ("_number", contacts[i].details[phoneKey])
                                .Build ();
                            OneTimeWorkRequest workRequest = OneTimeWorkRequest.Builder.From<SendMessageWorker> ()
                                .AddTag (intendedMessages[target].id)
                                .SetInitialDelay (intendedMessages[target].delayTime * i, Java.Util.Concurrent.TimeUnit.Milliseconds)
                                .SetInputData(messData)
                                .Build ();
                            WorkManager.Instance.Enqueue (workRequest);
                        }
                    }
                }
                if (async)
                {
                    Task.Run (() =>
                    {
                        DoWork ();
                        callback?.Invoke ();
                    });
                }
                else
                {
                    DoWork ();
                    callback?.Invoke ();
                }
            }
        }
    }
    [BroadcastReceiver (Enabled = true, Name = "com.xk.masstext.statereciever", Permission = "com.xk.masstext.ReciveBroadcasts")]
    [IntentFilter (new string[] { MessengerService.delivered, MessengerService.sent })]
    class MessageStateReciever : BroadcastReceiver
    {
        public override void OnReceive (Context context, Intent intent)
        {
            try
            {
                string id = intent.GetStringExtra ("_id");
                int index = intent.GetIntExtra ("_index", -1);
                if (index == -1 || id == null || id == string.Empty)
                {
                    return;
                }

                MessageStatus result = (MessageStatus)intent.GetIntExtra ("_result", (int)MessageStatus.Pending);
                List<IntendedMessages> allMessages = StorageManager.GetCollection<IntendedMessages> (nameof (IntendedMessages));

                List<IntendedMessages> res = (from IntendedMessages ints in allMessages
                                              where ints.id == id
                                              select ints).ToList ();
                allMessages.Remove (res[0]);

                res[0].statuses[index] = ((int)ResultCode) switch
                {
                    (int)Result.Ok => result,
                    _ => MessageStatus.Failed,
                };
                allMessages.Add (res[0]);
                StorageManager.CacheObject (allMessages, nameof (IntendedMessages));
                Android.Util.Log.Info (nameof (MassText), $"Data Retrieved Result = {res[0].statuses[index]}");
            }
            catch
            {

            }
        }
    }
    public class SendMessageWorker : Worker
    {
        public SendMessageWorker (Context context, WorkerParameters workerParams) : base (context, workerParams)
        {
            message = workerParams.InputData.GetString ("_message");
            id = workerParams.InputData.GetString ("_id");
            number = workerParams.InputData.GetString ("_number");
            index = workerParams.InputData.GetInt ("_index", -1);
            this.context = context;
        }
        string id;
        string message;
        string number;
        int index;
        Context context;
        public override Result DoWork ()
        {
            if (index == -1)
            {
                return Result.InvokeRetry ();
            }

            SmsManager manager = SmsManager.Default;
            Intent _sentIntent = new Intent (MessengerService.sent);
            _sentIntent.SetPackage ("com.xk.masstextapp");
            Intent _deliveredIntent = new Intent (MessengerService.delivered);
            _deliveredIntent.SetPackage ("com.xk.masstextapp");
            _sentIntent.PutExtra ("_id", id);
            _sentIntent.PutExtra ("_index", index);
            _sentIntent.PutExtra ("_result", (int)MessageStatus.Sent);
            _deliveredIntent.PutExtra ("_id", id);
            _deliveredIntent.PutExtra ("_index", index);
            _deliveredIntent.PutExtra ("_result", (int)MessageStatus.Delivered);
            PendingIntent sentIntent = PendingIntent.GetBroadcast (context, 0, _sentIntent, 0);
            PendingIntent deliveredIntent = PendingIntent.GetBroadcast (context, 0, _deliveredIntent, 0);
            List<string> divided = new List<string> (manager.DivideMessage (message));
            if (divided.Count > 1)
            {
                manager.SendMultipartTextMessage (PhoneNumberProcessor.Cleaned (number), null, divided, new List<PendingIntent> () { sentIntent }, new List<PendingIntent> () { deliveredIntent });
            }
            else
            {
                manager.SendTextMessage (PhoneNumberProcessor.Cleaned (number), null, message, sentIntent, deliveredIntent);
            }
            Android.Util.Log.Info ("MessageServiceWorker", $"Sent Message {message}");
            return Result.InvokeSuccess ();
        }
    }
    public class MessageServiceWorker : Worker
    {
        public MessageServiceWorker (Context context, WorkerParameters workerParams) : base (context, workerParams)
        {
            target = workerParams.InputData.GetString ("_id");
            Android.Util.Log.Debug ("MessageServiceWorker", "Init");
            this.context = context;
        }
        Context context;
        readonly string target;
        public override Result DoWork ()
        {
            Android.Util.Log.Debug ("MessageServiceWorker", $"Sending texts id: {target}"); SmsManager manager = SmsManager.Default;
            List<string> _workingIds = StorageManager.GetCollection<string> ("workingIds");
            _workingIds.Add (target);
            StorageManager.CacheObject (_workingIds, "workingIds");
            try
            {
                MessengerService.SendMessages (context, target, false, false, async: false, repeatcheck: (mess) =>
                    {
                        if (mess.repeat)
                        {
                            Data targetData = new Data.Builder ().PutString ("_id", mess.id).Build ();
                            OneTimeWorkRequest request = OneTimeWorkRequest.Builder.From<MessageServiceWorker> ()
                            .SetInitialDelay ((long)mess.repeatTime.TotalMilliseconds, Java.Util.Concurrent.TimeUnit.Milliseconds)
                            .SetInputData (targetData)
                            .AddTag (mess.id)
                            .Build ();
                            WorkManager.Instance.Enqueue (request);
                            Android.Util.Log.Debug ("MessageServiceWorker", $"Successfully requeued");
                        }
                    });
                _workingIds.Remove (target);
                StorageManager.CacheObject (_workingIds, "workingIds");
                Android.Util.Log.Debug ("MessageServiceWorker", $"Successfully sent messages");
                return Result.InvokeSuccess ();
            }
            catch
            {
                Android.Util.Log.Debug ("MessageServiceWorker", $"Failed to send messages");

                return Result.InvokeRetry ();
            }
        }
    }
}
