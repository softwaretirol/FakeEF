﻿using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Newtonsoft.Json;
using Formatting = System.Xml.Formatting;

namespace FakeEF
{
    public class InMemoryTable<T> : InMemoryTableBase, IEnumerable<T>
        where T : class
    {
        private static readonly InMemoryTable<T> instance = new InMemoryTable<T>();
        private readonly List<T> data = new List<T>();

        public static InMemoryTable<T> Instance
        {
            get { return instance; }
        }

        private InMemoryTable()
        {
            
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Instance.data.Concat(data).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SaveChangesInMemory(IEnumerable<DbEntityEntry<T>> notYetInDatabase)
        {
            foreach (var item in notYetInDatabase)
            {
                if (item.State == EntityState.Deleted)
                    data.Remove(item.Entity);

                if (item.State == EntityState.Added)
                {
                    SetId(item.Entity);
                    item.Property("Id").IsModified = false;
                    data.Add(item.Entity);
                }

                if (item.State == EntityState.Modified)
                {
                    var id = GetId(item.Entity);
                    var existing = data.FirstOrDefault(x => GetId(x).Equals(id));

                    foreach (var property in typeof (T).GetProperties())
                    {
                        if (property.PropertyType.IsPrimitive || property.PropertyType == typeof (string))
                        {
                            var newValue = property.GetValue(item.Entity);
                            property.SetValue(existing, newValue);
                        }
                    }
                }
            }
        }

        internal object GetId(object item)
        {
            var type = item.GetType();
            return type.GetProperty("Id").GetValue(item);
        }
        internal void SetId(object item)
        {
            var type = item.GetType();
            type.GetProperty("Id").SetValue(item, data.Count + 1);
        }

        public override void Clear()
        {
            data.Clear();
        }

        public IEnumerable<T> CloneItems()
        {
            return instance.data.Select(Clone);
        }
        public T Clone(T source)
        {
            var serialized = JsonConvert.SerializeObject(source, Newtonsoft.Json.Formatting.Indented,
                                new JsonSerializerSettings
                                {
                                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                });
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}