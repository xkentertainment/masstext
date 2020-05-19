using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MassText
{
    class StorageManager
    {
        public static string storagePath = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal) + "/";
        public static string cachePath = "_cache.pt";
        public static void CacheObject<T> (T obj, string cacheName)
        {
            FileStream file = File.Create (PathOf (cacheName));
            BinaryFormatter bf = new BinaryFormatter ();
            bf.Serialize (file, obj);
            file.Close ();
        }
        public static T GetCache<T> (string cacheName)
        {
            if (!File.Exists (PathOf (cacheName)))
            {
                return default;
            }

            FileStream file = File.Open (PathOf (cacheName), FileMode.Open);
            BinaryFormatter bf = new BinaryFormatter ();
            T obj = (T)bf.Deserialize (file);
            file.Close ();
            return obj;
        }
        public static void AddToCollection<T> (T obj, string collectionName, out int index)
        {
            if (!File.Exists (PathOf (collectionName)))
            {
                CreateCollection<T> (collectionName);
            }
            List<T> objs = GetCollection<T> (collectionName);
            objs.Add (obj);
            index = objs.Count - 1;
            CacheObject (objs, collectionName);
        }
        public static void AddToCollection<T> (T obj, string collectionName)
        {
            if (!File.Exists (PathOf (collectionName)))
            {
                CreateCollection<T> (collectionName);
            }
            List<T> objs = GetCollection<T> (collectionName);
            objs.Add (obj);
            CacheObject (objs, collectionName);
        }
        public static List<T> GetCollection<T>(string collectionName)
        {
            return GetCache<List<T>> (collectionName) ?? new List<T> ();
        }
        public static void CreateCollection<T> (string collectionName)
        {
            CacheObject (new List<T> (), collectionName);
        }
        public static void ClearCache (string cacheName)
        {
            if (File.Exists (PathOf (cacheName)))
            {
                File.Delete (PathOf(cacheName));
            }
        }
        static string PathOf(string cacheName)
        {
            return storagePath + cacheName + cachePath;
        }
    }
}