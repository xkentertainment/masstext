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

namespace MassText
{
    public static class IdGen
    {
        public static string Generate ()
        {
            return Convert.ToBase64String (Encoding.Default.GetBytes ((DateTime.Now.Ticks).ToString ())) + Convert.ToBase64String (Encoding.Default.GetBytes (new Random().NextDouble().ToString ()));
        }
        public static int ServiceId ()
        {
            return (int)DateTime.Now.Ticks + new Random ().Next ();
        }
    }
}