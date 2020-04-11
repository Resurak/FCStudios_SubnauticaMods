﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using FCS_HydroponicHarvesters.Enumerators;
using FCS_HydroponicHarvesters.Model;
using FCSCommon.Extensions;
using FCSCommon.Utilities;
using FCSTechFabricator.Abstract;
using FCSTechFabricator.Components;
using FCSTechFabricator.Enums;
using FCSTechFabricator.Interfaces;
using SMLHelper.V2.Handlers;
using UnityEngine;

namespace FCS_HydroponicHarvesters.Mono
{
    internal class HydroHarvGrowBed : MonoBehaviour, IFCSStorage
    {
        public PlantSlot[] Slots;
        private bool _initialized;
        private FCSEnvironment _currentEnvironment;
        
        private HydroHarvController _mono;
        private Dictionary<TechType,TechType>  _dna = new Dictionary<TechType,TechType>();
        private bool _fromLoad;

        internal void Initialize(HydroHarvController mono)
        {
            _mono = mono;

            if (FindPots())
            {
                _initialized = true;
            }
        }

        private bool FindPots()
        {
            try
            {
                var planters = gameObject.transform.Find("model").Find("Planters").transform;
                Slots = new PlantSlot[planters.childCount];

                for (int i = 0; i < planters.childCount; i++)
                {
                    Slots[i] = new PlantSlot(i, planters.GetChild(i));
                }
            }
            catch (Exception e)
            {
                QuickLogger.Error(e.Message);
                return false;
            }
            
            return true;
        }

        public int GetContainerFreeSpace => Slots.Count(x => !x.IsOccupied);
        public bool IsFull => GetContainerFreeSpace <= 0;

        public bool CanBeStored(int amount)
        {
            throw new NotImplementedException();
        }

        public bool AddItemToContainer(InventoryItem item)
        {
            var dna = item.item.gameObject.GetComponentInChildren<FCSDNA>();
            return TryPlant(dna,item);
        }

        internal HydroHarvSize GetHydroHarvSize()
        {
            switch (Slots.Length)
            {
                case 1:
                    return HydroHarvSize.Small;
                case 2:
                    return HydroHarvSize.Medium;
                case 4:
                    return HydroHarvSize.Large;
            }

            return HydroHarvSize.Unknown;
        }
        
        public bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            var dna = pickupable.GetComponent<FCSDNA>();

            if (dna == null)
            {
                return false;
            }

            if (IsFull)
            {
                return false;
            }
            
            dna.GetData();
            
            if (_currentEnvironment != dna.Environment)
            {
                return false;
            }

            return true;
        }

        private bool TryPlant(FCSDNA plantable, InventoryItem item)
        {
            bool result = false;
            if (IsCorrectEnvironment(plantable))
            {
                if (GetContainerFreeSpace >= 1)
                {
                    var freeslot = GetFreeSlotID();
                    AddItem(plantable, freeslot,item.item.GetTechType());
                    result =  true;
                }
            }

            if (result)
            {
                Destroy(item.item);
            }
            
            return result;
        }

        private void AddItem(FCSDNA plantable, int slotID,TechType dnaTechType)
        {
            var slotByID = this.GetSlotByID(slotID);
            if (slotByID == null)
            {
                return;
            }
            if (slotByID.IsOccupied)
            {
                return;
            }
            QuickLogger.Debug($"Adding DNa with TechType {plantable.TechType}",true);

            SetSlotOccupiedState(slotID,true);
            slotByID.Plantable = plantable;
            slotByID.PlantModel = Spawn(slotByID.Slot, plantable.TechType, plantable.Model);
            _dna.Add(plantable.GiveItem,dnaTechType);
            if (!_fromLoad)
            {
                if (!_mono.HydroHarvContainer.Items.ContainsKey(plantable.GiveItem))
                {
                    _mono.HydroHarvContainer.AddItemToContainer(plantable.GiveItem, true);
                }
            }
        }

        public GameObject Spawn(Transform parent, TechType techType,GameObject model)
        {
            var gameObject = model != null ? Instantiate(model) : Instantiate(CraftData.GetPrefabForTechType(techType));

            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localScale = GetModelScale(techType);
            gameObject.transform.localPosition = Vector3.zero;

            var growingPlant = gameObject.GetComponent<GrowingPlant>();
            if (growingPlant != null)
            {
                growingPlant.EnableIndoorState();
                growingPlant.enabled = false;
                growingPlant.SetProgress(1f);
                Transform transform = model.transform;
                growingPlant.SetScale(transform, 1f);
                growingPlant.SetPosition(transform);
            }
            else
            {
                Destroy(gameObject.GetComponent<Pickupable>());
                gameObject.AddComponent<TechTag>().type = techType;
                Destroy(gameObject.GetComponent<UniqueIdentifier>());
                gameObject.AddComponent<GrownPlant>().seed = null;
            }

            Destroy(gameObject.GetComponent<Rigidbody>());
            Destroy(gameObject.GetComponent<WorldForces>());

            SetActiveAllChildren(parent.transform, true);
            
            return gameObject;
        }

        private Vector3 GetModelScale(TechType techType)
        {
            return new Vector3(0.3f, 0.3f, 0.3f);
        }

        private void SetActiveAllChildren(Transform transform, bool value)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(value);

                SetActiveAllChildren(child, value);
            }
        }

        internal void RemoveItem(int slotID)
        {
            var slotByID = GetSlotByID(slotID);

            if (slotByID == null)
            {
                QuickLogger.Debug("SlotById returned null");
                return;
            }

            GameObject plantModel = slotByID.PlantModel;
            slotByID.Clear();
            Destroy(plantModel);
            SetSlotOccupiedState(slotID, false);
        }

        private void SetSlotOccupiedState(int slotID, bool state)
        {
            GetSlotByID(slotID).IsOccupied = state;
        }

        private int GetFreeSlotID()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (!Slots[i].IsOccupied)
                {
                    return i;
                }
            }

            return 0;
        }

        private PlantSlot GetSlotByID(int slotID)
        {
            return Slots[slotID];
        }

        private bool IsCorrectEnvironment(FCSDNA plantable)
        {
            if (_currentEnvironment == FCSEnvironment.Water && plantable.Environment == FCSEnvironment.Water)
            {
                return true;
            }

            if (_currentEnvironment == FCSEnvironment.Air && plantable.Environment == FCSEnvironment.Air)
            {
                return true;
            }

            return false;
        }

        internal void SetBedType(FCSEnvironment environment)
        {
            _currentEnvironment = environment;
        }
        
        internal FCSEnvironment GetBedType()
        {
            return _currentEnvironment;
        }
        internal Dictionary<TechType,TechType> GetDNASamples()
        {
            return _dna;
        }

        internal bool HasItems()
        {
            return _dna.Count > 0;
        }
        internal void RemoveDNA(TechType item)
        {
            if (_dna == null)
            {
                QuickLogger.Error("DNA is Null");
                return;
            }

            if (Slots == null)
            {
                QuickLogger.Error("Slots is Null");
                return;
            }
            _dna.Remove(item);

            foreach (PlantSlot slot in Slots)
            {
                if (slot?.Plantable?.GiveItem == item)
                {
                    RemoveItem(slot.Id);
                    break;
                }
            }

            QuickLogger.Debug("Remove Model");
        }
        internal void Load(Dictionary<TechType, TechType> savedDataDnaSamples)
        {
            if (savedDataDnaSamples == null) return;
            _fromLoad = true;
            foreach (KeyValuePair<TechType, TechType> dnaSample in savedDataDnaSamples)
            {
                var dna = dnaSample.Value.ToInventoryItem();
                var fcsDna = dna.item.gameObject.GetComponentInChildren<FCSDNA>();
                fcsDna.GetData();
                AddItemToContainer(dna);
            }
            _fromLoad = false;
        }
    }
}
