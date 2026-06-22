//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using static ITEMS;
//public class Craft
//{
//    public Dictionary<byte?[], Tuple<byte, int, ItemType>> sets = new()
//    {
//        #region Доски
//        { new byte?[] { null, 8 }, new(11, 4, ItemType.Block) },

//        { new byte?[] { 8, null }, new(11, 4, ItemType.Block) },

//        {
//            new byte?[] { null, null,
//                          8,  null },
//            new(11, 4, ItemType.Block)
//        },

//        {
//            new byte?[] {   8,  null,
//                        null, null },
//            new(11, 4, ItemType.Block)
//        },

//        {
//            new byte?[] { null,  8,
//                        null, null },
//            new(11, 4, ItemType.Block)
//        },

//        {
//            new byte?[] { null, null,
//                        null,   8  },
//            new(11, 4, ItemType.Block)
//        },
//        #endregion

//        #region Простой Верстак
//        { new byte?[] { 11, 11 }, new(100, 1, ItemType.Block) },

//        {
//            new byte?[] {  11,   11,
//                        null, null },
//            new(100, 1, ItemType.Block)
//        },

//        {
//            new byte?[] { null, null,
//                         11,   11  },
//            new(100, 1, ItemType.Block)
//        },

//        #endregion

//        #region Верстак
//        {
//            new byte?[] { 100, 100,
//                        100, 100 },
//            new(101, 1, ItemType.Block)
//        },
//        #endregion

//        #region Палки
//        {
//            new byte?[] { 11, null,
//                          11, null },
//            new(STICK, 4, ItemType.Item)
//        },

//        {
//            new byte?[] { null, 11,
//                          null, 11 },
//            new(STICK, 4, ItemType.Item)
//        },

//        #endregion

//        #region Печь
//        {
//            new byte?[] { 3,  3,  3,
//                          3, null,3,
//                          3,  3,  3 },
//            new(102, 1, ItemType.Block)
//        },
//        #endregion

//        #region Порох
//        {
//            new byte?[] { ITEMS.COAL,  ITEMS.SALTPETER,  ITEMS.SULFUR,
//                           null, null, null,
//                            null,  null,  null },
//            new(ITEMS.GUNPOWDER, 1, ItemType.Item)
//        },

//        {
//            new byte?[] { null, null, null,
//                        ITEMS.COAL,  ITEMS.SALTPETER,  ITEMS.SULFUR,
//                            null,  null,  null },
//            new(ITEMS.GUNPOWDER, 1, ItemType.Item)
//        },

//        {
//            new byte?[] { null, null, null,
//                          null, null, null,
//                ITEMS.COAL,  ITEMS.SALTPETER,  ITEMS.SULFUR },
//            new(ITEMS.GUNPOWDER, 1, ItemType.Item)
//        },
//        #endregion

//        #region Кусочек Железа
//        { new byte?[] { null, ITEMS.INGOT_IRON }, new(ITEMS.IRON_PART, 9, ItemType.Item) },
//        { new byte?[] { ITEMS.INGOT_IRON, null }, new(ITEMS.IRON_PART, 9, ItemType.Item) },
        
//        { new byte?[] { null, null, ITEMS.INGOT_IRON, null }, new(ITEMS.IRON_PART, 9, ItemType.Item) },
//        { new byte?[] { null, ITEMS.INGOT_IRON, null, null }, new(ITEMS.IRON_PART, 9, ItemType.Item) },
//        { new byte?[] { ITEMS.INGOT_IRON, null }, new(ITEMS.IRON_PART, 9, ItemType.Item) },
//        { new byte?[] { null, null, null, ITEMS.INGOT_IRON }, new(ITEMS.IRON_PART, 9, ItemType.Item) },

//        #endregion

//        #region Патрон
//        { new byte?[] { GUNPOWDER, IRON_PART }, new(BULLET, 8, ItemType.Item) },
//        { new byte?[] { GUNPOWDER, IRON_PART, 
//                            null,    null }, new(BULLET, 8, ItemType.Item) },
//        { new byte?[] {   null,      null, 
//                        GUNPOWDER, IRON_PART }, new(BULLET, 8, ItemType.Item) },
//        { new byte?[] { GUNPOWDER, IRON_PART, null,
//                           null,     null,    null,
//                           null,     null,    null}, new(BULLET, 8, ItemType.Item) },

//        #endregion

//        #region Обойма
//        { new byte?[] { INGOT_IRON, BULLET, INGOT_IRON,
//                        INGOT_IRON, BULLET, INGOT_IRON,
//                        INGOT_IRON, BULLET, INGOT_IRON}, new(MAGAZINE, 1, ItemType.Item) },
//        #endregion

//        #region Простой Пистолет
//        {
//            new byte?[] {    null,       null,     null,
//                          INGOT_IRON, INGOT_IRON, SILICON,
//                            null,        null,    MAGAZINE}, new(SIMPLE_PISTOL, 1, ItemType.Item)
//        },

//        {
//            new byte?[] { INGOT_IRON, INGOT_IRON, SILICON,
//                            null,        null,    MAGAZINE,
//                            null,        null,      null},
//                                                             new(SIMPLE_PISTOL, 1, ItemType.Item)
//        },
//        #endregion

//        #region Деревянный топор
//        {
//            new byte?[] { STICK, 11,
//                          STICK, null },
//            new(AXE_WOODEN, 1, ItemType.Item)
//        },

//        {
//            new byte?[] {  11,  STICK, 
//                          null, STICK },
//            new(AXE_WOODEN, 1, ItemType.Item)
//        },
//        #endregion
//    };

//    // id блока/предмета и время горения
//    public Dictionary<byte, float> setsCombustible = new()
//    {
//        {  8,  39 },
//        {  11, 10 },
//        { COAL, 10 },
//    };

//    // id блока/предмета и время обработки огнем
//    public Dictionary<byte, Furnaceable> setsFurnaceable = new()
//    {
//        { 30, new(5, INGOT_IRON) },
//    };

//    public struct Furnaceable
//    {
//        public int fireTime;
//        public byte itemID;

//        public Furnaceable(int ft, byte id)
//        {
//            fireTime = ft;
//            itemID = id;
//        }
//    }
//}
