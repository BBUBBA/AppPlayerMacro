﻿using Macro.Infrastructure.Serialize;
using Macro.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Utils;

namespace Macro.Infrastructure.Manager
{
    public class CacheDataManager
    {
        private CacheModel _cacheData;
        private int _atomic = 0;
        private readonly Dictionary<ulong, EventTriggerModel> _indexTriggerModels;
        public CacheDataManager()
        {
            _indexTriggerModels = new Dictionary<ulong, EventTriggerModel>();

            _cacheData = new CacheModel(0);

            NotifyHelper.EventTriggerInserted += NotifyHelper_EventTriggerInserted;
            NotifyHelper.EventTriggerRemoved += NotifyHelper_EventTriggerRemoved;
        }

        public bool CheckAndMakeCacheFile(List<EventTriggerModel> saves, string path)
        {
            var isNewCreated = false;
            var fullPath = $@"{path}{ConstHelper.DefaultCacheFile}";
            var isExists = File.Exists(fullPath);
            if (isExists && saves.Count > 0)
            {
                var bytes = File.ReadAllBytes(fullPath);
                _cacheData = ObjectSerializer.DeserializeObject<CacheModel>(bytes).FirstOrDefault();
            }
            else
            {
                isNewCreated = true;
            }
            foreach (var save in saves)
            {
                MakeIndexTriggerModel(save);
                InsertIndexTriggerModel(save);
            }
            UpdateCacheData(fullPath);
            return isNewCreated;
        }
        public void UpdateCacheData(string path)
        {
            _cacheData.LatestCheckDateTime = DateTime.Now.Ticks;

            var bytes = ObjectSerializer.SerializeObject(_cacheData);
            File.WriteAllBytes(path, bytes);
        }
        public bool IsUpdated()
        {
            return TimeSpan.FromTicks(DateTime.Now.Ticks - _cacheData.LatestCheckDateTime).TotalDays > 1;
        }
        public ulong GetMaxIndex()
        {
            return _cacheData.MaxIndex;
        }
        public ulong IncreaseIndex()
        {
            if(Interlocked.Exchange(ref _atomic, 1) == 0)
            {
                _cacheData.MaxIndex++;
                Interlocked.Exchange(ref _atomic, 0);
            }
            return _cacheData.MaxIndex;
        }

        public EventTriggerModel GetEventTriggerModel(ulong index)
        {
            if(_indexTriggerModels.ContainsKey(index))
            {
                return _indexTriggerModels[index];
            }
            else
            {
                return null;
            }
        }
        private void NotifyHelper_EventTriggerRemoved(EventTriggerEventArgs obj)
        {
            RemoveIndexTriggerModel(obj.TriggerModel);
        }

        private void NotifyHelper_EventTriggerInserted(EventTriggerEventArgs obj)
        {
            InsertIndexTriggerModel(obj.TriggerModel);
        }
        public void MakeIndexTriggerModel(EventTriggerModel model)
        {
            if (model.TriggerIndex == 0)
            {
                model.TriggerIndex = IncreaseIndex();
            }
            else if (model.TriggerIndex > _cacheData.MaxIndex)
            {
                _cacheData.MaxIndex = model.TriggerIndex;
            }
            foreach (var child in model.SubEventTriggers)
            {
                MakeIndexTriggerModel(child);
            }
        }
        private void InsertIndexTriggerModel(EventTriggerModel model)
        {
            if (_indexTriggerModels.ContainsKey(model.TriggerIndex) == false)
            {
                _indexTriggerModels.Add(model.TriggerIndex, model);
            }
            foreach (var child in model.SubEventTriggers)
            {
                InsertIndexTriggerModel(child);
            }
        }
        
        private void RemoveIndexTriggerModel(EventTriggerModel model)
        {
            if (_indexTriggerModels.ContainsKey(model.TriggerIndex))
            {
                _indexTriggerModels.Remove(model.TriggerIndex);
            }
            foreach (var child in model.SubEventTriggers)
            {
                RemoveIndexTriggerModel(child);
            }
        }
    }
}