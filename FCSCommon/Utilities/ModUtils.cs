﻿using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Oculus.Newtonsoft.Json;

namespace FCSCommon.Utilities
{
    public static class ModUtils
    {

        private static MonoBehaviour _coroutineObject;


        public static void Save<SaveDataT>(SaveDataT newSaveData, string fileName, string saveDirectory, Action onSaveComplete = null)
        {
            if (newSaveData != null)
            {
                var saveDataJson = JsonConvert.SerializeObject(newSaveData);
                
                File.WriteAllText(Path.Combine(saveDirectory,fileName), saveDataJson);

                onSaveComplete?.Invoke();
            }
        }

        public static void LoadSaveData<TSaveData>(string fileName, string saveDirectory, Action<TSaveData> onSuccess) where TSaveData : new()
        {
            var save = File.ReadAllText(Path.Combine(saveDirectory, fileName));
            var json = JsonConvert.DeserializeObject<TSaveData>(save);
            onSuccess?.Invoke(json);
        }

        private static Coroutine StartCoroutine(IEnumerator coroutine)
        {
            if (_coroutineObject == null)
            {
                var go = new GameObject();
                _coroutineObject = go.AddComponent<ModSaver>();
            }

            return _coroutineObject.StartCoroutine(coroutine);
        }
    }
}