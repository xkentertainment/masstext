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
using Android.App.Job;
using System.Threading.Tasks;

//namespace MassText
//{
//    [Service (Name = "com.xk.masstextapp.messagingjobs.scheduled", Permission = "android.permission.BIND_JOB_SERVICE", Exported = true)]
//    public class ScheduledMessagingJob : JobService
//    {
//        public override bool OnStartJob (JobParameters @params)
//        {
//            Task.Run (() =>
//            {
//                string targetId = @params.Extras.GetString ("_id");
//                List<string> workingIds = StorageManager.GetCollection<string> (nameof (workingIds));

//                if (!workingIds.Contains (targetId))
//                {
//                    StorageManager.AddToCollection (targetId, nameof (workingIds));
//                    SendMessages.SendMessagesNowCompat (this, targetId);
//                }
//                Toast.MakeText (this, "job vee runnin", ToastLength.Long).Show ();
//                JobFinished (@params, true);
//            });
//            return true;
//        }

//        public override bool OnStopJob (JobParameters @params)
//        {
//            return true;
//        }
//    }
//    public static class JobSchedulerHelpers
//    {
//        public static JobInfo.Builder CreateJobBuilderUsingJobId<T> (this Context context, int jobId)
//        {
//            var javaClass = Java.Lang.Class.FromType (typeof (T));
//            var componentName = new ComponentName (context, javaClass);
//            return new JobInfo.Builder (jobId, componentName);
//        }
//    }
//}