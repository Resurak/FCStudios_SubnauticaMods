﻿using System;
using System.Collections.Generic;
using System.Linq;
using FCSCommon.Extensions;
using FCSCommon.Utilities;
using FCSTechFabricator.Abstract;
using FCSTechFabricator.Enums;
using FCSTechFabricator.Objects;
using UnityEngine;

namespace FCSTechFabricator.Components
{
    public class Fridge : MonoBehaviour
    { 
        private int _itemLimit;
        public Action<int, int> OnContainerUpdate;
        private bool _decay;
        private ModModes _modMode;
        public bool IsFull => NumberOfItems >= _itemLimit;
        public int NumberOfItems => FridgeItems.Count;
        
        private void AttemptDecay()
        {
            if (_decay && _modMode == ModModes.HardCore)
            {
                foreach (EatableEntities eatableEntities in FridgeItems)
                {
                    eatableEntities.IterateRotten();
                }
            }
        }

        public List<EatableEntities> FridgeItems { get; private set; } = new List<EatableEntities>();
        
        private EatableEntities FindMatch(TechType techType, EatableType eatableType)
        {
            switch (eatableType)
            {
                case EatableType.Rotten:
                    return FridgeItems.FirstOrDefault(x => (x.GetFoodValue() < 0 && x.GetWaterValue() < 0) && x.TechType == techType);
                default:
                    return FridgeItems.FirstOrDefault(x => (x.GetFoodValue() > 0 || x.GetWaterValue() > 0) && x.TechType == techType);
            }
        }

        public void Initialize(FCSController mono, int itemLimit)
        {
            _itemLimit = itemLimit;
            float time = UnityEngine.Random.Range(0f, 5f);
            InvokeRepeating(nameof(AttemptDecay), time, 5f);
        }
        
        public void AddItem(InventoryItem item,bool fromSave = false, float timeDecayPause = 0)
        {
            if (IsFull)
            {
                return;
            }
            
            var eatableEntity = new EatableEntities();
            eatableEntity.Initialize(item.item);
            eatableEntity.PauseDecay();
            if (fromSave)
            {
                eatableEntity.TimeDecayPause = timeDecayPause;
            }
            FridgeItems.Add(eatableEntity);
            OnContainerUpdate?.Invoke(NumberOfItems, _itemLimit);
            Destroy(item.item);
        }

        public void RemoveItem(TechType techType, EatableType eatableType)
        {
            
            var pickupable = techType.ToPickupable();

            if (Inventory.main.HasRoomFor(pickupable))
            {
                EatableEntities match = FindMatch(techType,eatableType);

                if (match != null)
                {
                    var go = GameObject.Instantiate(CraftData.GetPrefabForTechType(techType));
                    var eatable = go.GetComponent<Eatable>();
                    var pickup = go.GetComponent<Pickupable>();

                    match.UnpauseDecay();
                    eatable.timeDecayStart = match.TimeDecayStart;

                    if (Inventory.main.Pickup(pickup))
                    {
                        QuickLogger.Debug($"Removed Match Before || Fridge Count {FridgeItems.Count}");
                        FridgeItems.Remove(match);
                        QuickLogger.Debug($"Removed Match || Fridge Count {FridgeItems.Count}");
                    }
                    GameObject.Destroy(pickupable);
                    OnContainerUpdate?.Invoke(NumberOfItems, _itemLimit);
                }
            }
        }

        public bool IsEmpty()
        {
            return NumberOfItems <= 0;
        }

        public int GetAmount(TechType techType)
        {
            QuickLogger.Debug($"Getting amount for: {techType} || Fridge amount: {FridgeItems.Count}");
            return FridgeItems.Count(x => x.TechType == techType);
        }

        public List<EatableEntities> Save()
        {
            return FridgeItems;
        }

        public void LoadSave(List<EatableEntities> save)
        {
            foreach (EatableEntities eatableEntities in save)
            {
                QuickLogger.Debug($"Adding entity {eatableEntities.Name}");

                var food = GameObject.Instantiate(CraftData.GetPrefabForTechType(eatableEntities.TechType));

                var eatable = food.gameObject.GetComponent<Eatable>();
                eatable.timeDecayStart = eatableEntities.TimeDecayStart;

#if SUBNAUTICA
                var item = new InventoryItem(food.gameObject.GetComponent<Pickupable>().Pickup(false));
#elif BELOWZERO
                Pickupable pickupable = food.gameObject.GetComponent<Pickupable>();
                pickupable.Pickup(false);
                var item = new InventoryItem(pickupable);
#endif

                AddItem(item,true,eatableEntities.TimeDecayPause);

                QuickLogger.Debug(
                    $"Load Item {item.item.name}|| Decompose: {eatable.decomposes} || DRate: {eatable.kDecayRate}");
            }
        }

        public void SetDecay(bool value)
        {
                _decay = value;
            
            if (_decay && _modMode == ModModes.HardCore)
            {
                foreach (EatableEntities fridgeItem in FridgeItems)
                {
                    fridgeItem.UnpauseDecay();
                }
            }
            else
            {
                foreach (EatableEntities fridgeItem in FridgeItems)
                {
                    fridgeItem.PauseDecay();
                }
            }
        }

        public void SetModMode(ModModes modMode)
        {
            _modMode = modMode;
        }
    }
}