using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MassText
{
    public static class PhoneNumberProcessor
    {
        public static string Cleaned(string number)
        {
            if (number.Length < 4)
            {
                return string.Empty;
            }


            number = Regex.Replace (number, "[^0-9]", string.Empty);
            if (number.Remove (3) == "255")
            {
                number = number.Replace ("255", string.Empty);
            }
            if (number[0] != '0' && number[0] != '+')
            {
                number = $"0{number}";
            }
            return (number.Length != 10) ? string.Empty : number;
        }
    }
}