using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace MassText
{
    class ContactList : ArrayAdapter
    {
        public List<bool> @checked;
        public ContactList (Context context, int resource, IList objects, List<bool> @checked) : base (context, resource, objects)
        {
            this.@checked = @checked;
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            View view = base.GetView (position, convertView, parent);
            if (view is CheckBox)
            {
                (view as CheckBox).Checked = @checked[position];
            }

            return view;
        }
    }
}