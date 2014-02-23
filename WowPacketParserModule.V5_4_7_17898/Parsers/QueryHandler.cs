using System;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;
using Guid = WowPacketParser.Misc.Guid;

namespace WowPacketParserModule.V5_4_7_17898.Parsers
{
    public static class QueryHandler
    {
        [HasSniffData]
        [Parser(Opcode.SMSG_CREATURE_QUERY_RESPONSE)]
        public static void HandleCreatureQueryResponse(Packet packet)
        {
            var entry = packet.ReadEntry("Entry"); // +5
            var creature = new UnitTemplate();
            var hasData = packet.ReadBit(); //+16
            if (!hasData)
                return; // nothing to do

            creature.RacialLeader = packet.ReadBit("Racial Leader"); //+68
            var bits2C = packet.ReadBits(6); //+136

            var stringLens = new int[4][];
            for (var i = 0; i < 4; i++)
            {
                stringLens[i] = new int[2];
                stringLens[i][1] = (int)packet.ReadBits(11);
                stringLens[i][0] = (int)packet.ReadBits(11);
            }

            var qItemCount = packet.ReadBits(22); //+72
            var bits24 = packet.ReadBits(11); //+7
            var bits1C = (int)packet.ReadBits(11); //+9

            creature.Modifier2 = packet.ReadSingle("Modifier 2"); //+132

            
            var name = new string[4];
            for (var i = 0; i < 4; ++i)
            {
                if (stringLens[i][0] > 1)
                    name[i] = packet.ReadCString("Name", i);
                if (stringLens[i][1] > 1)
                    packet.ReadCString("Female Name", i);
            }
            creature.Name = name[0];
            creature.Modifier1 = packet.ReadSingle("Modifier 1"); //+15

            creature.DisplayIds = new uint[4];
            creature.KillCredits = new uint[2];

            creature.KillCredits[1] = packet.ReadUInt32("Kill Credit 2"); //+28
            creature.DisplayIds[2] = packet.ReadUInt32("Display ID 2"); //+31

            creature.QuestItems = new uint[qItemCount];
            for (var i = 0; i < qItemCount; ++i)
                creature.QuestItems[i] = (uint)packet.ReadEntryWithName<Int32>(StoreNameType.Item, "Quest Item", i); //+72

            creature.Type = packet.ReadEnum<CreatureType>("Type", TypeCode.Int32); //+12

            if (bits2C > 1)
                creature.IconName = packet.ReadCString("Icon Name"); //+100

            creature.TypeFlags = packet.ReadEnum<CreatureTypeFlag>("Type Flags", TypeCode.UInt32);
            creature.TypeFlags2 = packet.ReadUInt32("Creature Type Flags 2"); // Missing enum
            creature.KillCredits[0] = packet.ReadUInt32("Kill Credit 1"); //+27
            creature.Family = packet.ReadEnum<CreatureFamily>("Family", TypeCode.Int32); //+13
            creature.MovementId = packet.ReadUInt32("Movement ID"); //+23
            creature.Expansion = packet.ReadEnum<ClientType>("Expansion", TypeCode.UInt32); //+24
            creature.DisplayIds[0] = packet.ReadUInt32("Display ID 0"); //+29
            creature.DisplayIds[1] = packet.ReadUInt32("Display ID 1"); //+30       
            
            if (bits24 > 1)
                packet.ReadCString("String1C");

            creature.Rank = packet.ReadEnum<CreatureRank>("Rank", TypeCode.Int32); //+14

            if (bits1C > 1)
                creature.SubName = packet.ReadCString("Sub Name");

            creature.DisplayIds[3] = packet.ReadUInt32("Display ID 3"); //+32

            packet.AddSniffData(StoreNameType.Unit, entry.Key, "QUERY_RESPONSE");

            Storage.UnitTemplates.Add((uint)entry.Key, creature, packet.TimeSpan);

            var objectName = new ObjectName
            {
                ObjectType = ObjectType.Unit,
                Name = creature.Name,
            };
            Storage.ObjectNames.Add((uint)entry.Key, objectName, packet.TimeSpan);
        }

        [Parser(Opcode.CMSG_CREATURE_QUERY)]
        public static void HandleCreatureQuery(Packet packet)
        {
            packet.ReadInt32("Entry");
        }

        
        [HasSniffData]
        [Parser(Opcode.SMSG_DB_REPLY)]
        public static void HandleDBReply(Packet packet)
        {
            var entry = (uint)packet.ReadInt32("Entry");

            var type = packet.ReadEnum<DB2Hash>("DB2 File", TypeCode.UInt32);

            packet.ReadTime("Hotfix date");

            var size = packet.ReadInt32("Size");
            var data = packet.ReadBytes(size);
            var db2File = new Packet(data, packet.Opcode, packet.Time, packet.Direction, packet.Number, packet.Writer, packet.FileName);

            if ((int)entry < 0)
            {
                packet.WriteLine("Row {0} has been removed.", -(int)entry);
                return;
            }

            switch (type)
            {
                case DB2Hash.BroadcastText:
                    {
                        var broadcastText = new BroadcastText();

                        var Id = db2File.ReadEntry("Broadcast Text Entry");
                        broadcastText.language = db2File.ReadUInt32("Language");
                        if (db2File.ReadUInt16() > 0)
                            broadcastText.MaleText = db2File.ReadCString("Male Text");
                        if (db2File.ReadUInt16() > 0)
                            broadcastText.FemaleText = db2File.ReadCString("Female Text");

                        broadcastText.EmoteID = new uint[3];
                        broadcastText.EmoteDelay = new uint[3];
                        for (var i = 0; i < 3; ++i)
                            broadcastText.EmoteID[i] = (uint)db2File.ReadInt32("Emote ID", i);
                        for (var i = 0; i < 3; ++i)
                            broadcastText.EmoteDelay[i] = (uint)db2File.ReadInt32("Emote Delay", i);

                        broadcastText.soundId = db2File.ReadUInt32("Sound Id");
                        broadcastText.unk1 = db2File.ReadUInt32("Unk 1"); // emote unk
                        broadcastText.unk2 = db2File.ReadUInt32("Unk 2"); // kind of type?

                        Storage.BroadcastTexts.Add((uint)Id.Key, broadcastText, packet.TimeSpan);
                        break;
                    }
                case DB2Hash.Creature: // New structure - 5.4
                    {
                        db2File.ReadUInt32("Creature Entry");
                        db2File.ReadUInt32("Item Entry 1");
                        db2File.ReadUInt32("Item Entry 2");
                        db2File.ReadUInt32("Item Entry 3");
                        db2File.ReadUInt32("Mount");
                        for (var i = 0; i < 4; ++i)
                            db2File.ReadInt32("Display Id", i);

                        for (var i = 0; i < 4; ++i)
                            db2File.ReadSingle("Display Id Probability", i);

                        if (db2File.ReadUInt16() > 0)
                            db2File.ReadCString("Name");

                        if (db2File.ReadUInt16() > 0)
                            db2File.ReadCString("Sub Name");

                        if (db2File.ReadUInt16() > 0)
                            db2File.ReadCString("Unk String");

                        db2File.ReadUInt32("Rank");
                        db2File.ReadUInt32("Inhabit Type");
                        break;
                    }
                case DB2Hash.CreatureDifficulty:
                    {
                        db2File.ReadUInt32("Id");
                        db2File.ReadUInt32("Creature Entry");
                        db2File.ReadUInt32("Faction Template Id");
                        db2File.ReadUInt32("Expansion HP");
                        db2File.ReadUInt32("Min Level");
                        db2File.ReadUInt32("Max Level");
                        db2File.ReadUInt32("Unk 1");
                        db2File.ReadUInt32("Unk 2");
                        db2File.ReadUInt32("Unk 3");
                        db2File.ReadUInt32("Unk 4");
                        db2File.ReadUInt32("Unk 5");
                        break;
                    }
                case DB2Hash.GameObjects:
                    {
                        db2File.ReadUInt32("Gameobject Entry");
                        db2File.ReadUInt32("Map");
                        db2File.ReadUInt32("Display Id");
                        db2File.ReadSingle("Position X");
                        db2File.ReadSingle("Position Y");
                        db2File.ReadSingle("Position Z");
                        db2File.ReadSingle("Rotation 1");
                        db2File.ReadSingle("Rotation 2");
                        db2File.ReadSingle("Rotation 3");
                        db2File.ReadSingle("Rotation 4");
                        db2File.ReadSingle("Size");
                        db2File.ReadUInt32("Type");
                        db2File.ReadUInt32("Data 0");
                        db2File.ReadUInt32("Data 1");
                        db2File.ReadUInt32("Data 2");
                        db2File.ReadUInt32("Data 3");

                        if (db2File.ReadUInt16() > 0)
                            db2File.ReadCString("Name");
                        break;
                    }
                case DB2Hash.Item:
                    {
                        var item = Storage.ItemTemplates.ContainsKey(entry) ? Storage.ItemTemplates[entry].Item1 : new ItemTemplate();

                        db2File.ReadEntryWithName<UInt32>(StoreNameType.Item, "Item Entry");
                        item.Class = db2File.ReadEnum<ItemClass>("Class", TypeCode.Int32);
                        item.SubClass = db2File.ReadUInt32("Sub Class");
                        item.SoundOverrideSubclass = db2File.ReadInt32("Sound Override Subclass");
                        item.Material = db2File.ReadEnum<Material>("Material", TypeCode.Int32);
                        item.DisplayId = db2File.ReadUInt32("Display ID");
                        item.InventoryType = db2File.ReadEnum<InventoryType>("Inventory Type", TypeCode.UInt32);
                        item.SheathType = db2File.ReadEnum<SheathType>("Sheath Type", TypeCode.Int32);

                        Storage.ItemTemplates.Add(entry, item, packet.TimeSpan);
                        packet.AddSniffData(StoreNameType.Item, (int)entry, "DB_REPLY");
                        break;
                    }
                case DB2Hash.ItemExtendedCost:
                    {
                        db2File.ReadUInt32("Item Extended Cost Entry");
                        db2File.ReadUInt32("Required Honor Points");
                        db2File.ReadUInt32("Required Arena Points");
                        db2File.ReadUInt32("Required Arena Slot");
                        for (var i = 0; i < 5; ++i)
                            db2File.ReadUInt32("Required Item", i);

                        for (var i = 0; i < 5; ++i)
                            db2File.ReadUInt32("Required Item Count", i);

                        db2File.ReadUInt32("Required Personal Arena Rating");
                        db2File.ReadUInt32("Item Purchase Group");
                        for (var i = 0; i < 5; ++i)
                            db2File.ReadUInt32("Required Currency", i);

                        for (var i = 0; i < 5; ++i)
                            db2File.ReadUInt32("Required Currency Count", i);

                        db2File.ReadUInt32("Required Faction Id");
                        db2File.ReadUInt32("Required Faction Standing");
                        db2File.ReadUInt32("Requirement Flags");
                        db2File.ReadUInt32("Required Guild Level");
                        db2File.ReadUInt32("Required Achievement");
                        break;
                    }
                case DB2Hash.Item_sparse:
                    {
                        var item = Storage.ItemTemplates.ContainsKey(entry) ? Storage.ItemTemplates[entry].Item1 : new ItemTemplate();

                        db2File.ReadEntryWithName<UInt32>(StoreNameType.Item, "Item Sparse Entry");
                        item.Quality = db2File.ReadEnum<ItemQuality>("Quality", TypeCode.Int32);
                        item.Flags1 = db2File.ReadEnum<ItemProtoFlags>("Flags 1", TypeCode.UInt32);
                        item.Flags2 = db2File.ReadEnum<ItemFlagExtra>("Flags 2", TypeCode.Int32);
                        item.Flags3 = db2File.ReadUInt32("Flags 3");
                        item.Unk430_1 = db2File.ReadSingle("Unk430_1");
                        item.Unk430_2 = db2File.ReadSingle("Unk430_2");
                        item.BuyCount = db2File.ReadUInt32("Buy count");
                        item.BuyPrice = db2File.ReadUInt32("Buy Price");
                        item.SellPrice = db2File.ReadUInt32("Sell Price");
                        item.InventoryType = db2File.ReadEnum<InventoryType>("Inventory Type", TypeCode.Int32);
                        item.AllowedClasses = db2File.ReadEnum<ClassMask>("Allowed Classes", TypeCode.Int32);
                        item.AllowedRaces = db2File.ReadEnum<RaceMask>("Allowed Races", TypeCode.Int32);
                        item.ItemLevel = db2File.ReadUInt32("Item Level");
                        item.RequiredLevel = db2File.ReadUInt32("Required Level");
                        item.RequiredSkillId = db2File.ReadUInt32("Required Skill ID");
                        item.RequiredSkillLevel = db2File.ReadUInt32("Required Skill Level");
                        item.RequiredSpell = (uint)db2File.ReadEntryWithName<Int32>(StoreNameType.Spell, "Required Spell");
                        item.RequiredHonorRank = db2File.ReadUInt32("Required Honor Rank");
                        item.RequiredCityRank = db2File.ReadUInt32("Required City Rank");
                        item.RequiredRepFaction = db2File.ReadUInt32("Required Rep Faction");
                        item.RequiredRepValue = db2File.ReadUInt32("Required Rep Value");
                        item.MaxCount = db2File.ReadInt32("Max Count");
                        item.MaxStackSize = db2File.ReadInt32("Max Stack Size");
                        item.ContainerSlots = db2File.ReadUInt32("Container Slots");

                        item.StatTypes = new ItemModType[10];
                        for (var i = 0; i < 10; i++)
                        {
                            var statType = db2File.ReadEnum<ItemModType>("Stat Type", TypeCode.Int32, i);
                            item.StatTypes[i] = statType == ItemModType.None ? ItemModType.Mana : statType; // TDB
                        }

                        item.StatValues = new int[10];
                        for (var i = 0; i < 10; i++)
                            item.StatValues[i] = db2File.ReadInt32("Stat Value", i);

                        item.StatUnk1 = new int[10];
                        for (var i = 0; i < 10; i++)
                            item.StatUnk1[i] = db2File.ReadInt32("Unk UInt32 1", i);

                        item.StatUnk2 = new int[10];
                        for (var i = 0; i < 10; i++)
                            item.StatUnk2[i] = db2File.ReadInt32("Unk UInt32 2", i);

                        item.ScalingStatDistribution = db2File.ReadInt32("Scaling Stat Distribution");
                        item.DamageType = db2File.ReadEnum<DamageType>("Damage Type", TypeCode.Int32);
                        item.Delay = db2File.ReadUInt32("Delay");
                        item.RangedMod = db2File.ReadSingle("Ranged Mod");

                        item.TriggeredSpellIds = new int[5];
                        for (var i = 0; i < 5; i++)
                            item.TriggeredSpellIds[i] = db2File.ReadEntryWithName<Int32>(StoreNameType.Spell, "Triggered Spell ID", i);

                        item.TriggeredSpellTypes = new ItemSpellTriggerType[5];
                        for (var i = 0; i < 5; i++)
                            item.TriggeredSpellTypes[i] = db2File.ReadEnum<ItemSpellTriggerType>("Trigger Spell Type", TypeCode.Int32, i);

                        item.TriggeredSpellCharges = new int[5];
                        for (var i = 0; i < 5; i++)
                            item.TriggeredSpellCharges[i] = db2File.ReadInt32("Triggered Spell Charges", i);

                        item.TriggeredSpellCooldowns = new int[5];
                        for (var i = 0; i < 5; i++)
                            item.TriggeredSpellCooldowns[i] = db2File.ReadInt32("Triggered Spell Cooldown", i);

                        item.TriggeredSpellCategories = new uint[5];
                        for (var i = 0; i < 5; i++)
                            item.TriggeredSpellCategories[i] = db2File.ReadUInt32("Triggered Spell Category", i);

                        item.TriggeredSpellCategoryCooldowns = new int[5];
                        for (var i = 0; i < 5; i++)
                            item.TriggeredSpellCategoryCooldowns[i] = db2File.ReadInt32("Triggered Spell Category Cooldown", i);

                        item.Bonding = db2File.ReadEnum<ItemBonding>("Bonding", TypeCode.Int32);

                        if (db2File.ReadUInt16() > 0)
                            item.Name = db2File.ReadCString("Name", 0);

                        for (var i = 1; i < 4; ++i)
                            if (db2File.ReadUInt16() > 0)
                                db2File.ReadCString("Name", i);

                        if (db2File.ReadUInt16() > 0)
                            item.Description = db2File.ReadCString("Description");

                        item.PageText = db2File.ReadUInt32("Page Text");
                        item.Language = db2File.ReadEnum<Language>("Language", TypeCode.Int32);
                        item.PageMaterial = db2File.ReadEnum<PageMaterial>("Page Material", TypeCode.Int32);
                        item.StartQuestId = (uint)db2File.ReadEntryWithName<Int32>(StoreNameType.Quest, "Start Quest");
                        item.LockId = db2File.ReadUInt32("Lock ID");
                        item.Material = db2File.ReadEnum<Material>("Material", TypeCode.Int32);
                        item.SheathType = db2File.ReadEnum<SheathType>("Sheath Type", TypeCode.Int32);
                        item.RandomPropery = db2File.ReadInt32("Random Property");
                        item.RandomSuffix = db2File.ReadUInt32("Random Suffix");
                        item.ItemSet = db2File.ReadUInt32("Item Set");
                        item.AreaId = (uint)db2File.ReadEntryWithName<UInt32>(StoreNameType.Area, "Area");
                        // In this single (?) case, map 0 means no map
                        var map = db2File.ReadInt32();
                        item.MapId = map;
                        db2File.WriteLine("Map ID: " + (map != 0 ? StoreGetters.GetName(StoreNameType.Map, map) : map + " (No map)"));
                        item.BagFamily = db2File.ReadEnum<BagFamilyMask>("Bag Family", TypeCode.Int32);
                        item.TotemCategory = db2File.ReadEnum<TotemCategory>("Totem Category", TypeCode.Int32);

                        item.ItemSocketColors = new ItemSocketColor[3];
                        for (var i = 0; i < 3; i++)
                            item.ItemSocketColors[i] = db2File.ReadEnum<ItemSocketColor>("Socket Color", TypeCode.Int32, i);

                        item.SocketContent = new uint[3];
                        for (var i = 0; i < 3; i++)
                            item.SocketContent[i] = db2File.ReadUInt32("Socket Item", i);

                        item.SocketBonus = db2File.ReadInt32("Socket Bonus");
                        item.GemProperties = db2File.ReadInt32("Gem Properties");
                        item.ArmorDamageModifier = db2File.ReadSingle("Armor Damage Modifier");
                        item.Duration = db2File.ReadUInt32("Duration");
                        item.ItemLimitCategory = db2File.ReadInt32("Limit Category");
                        item.HolidayId = db2File.ReadEnum<Holiday>("Holiday", TypeCode.Int32);
                        item.StatScalingFactor = db2File.ReadSingle("Stat Scaling Factor");
                        item.CurrencySubstitutionId = db2File.ReadUInt32("Currency Substitution Id");
                        item.CurrencySubstitutionCount = db2File.ReadUInt32("Currency Substitution Count");

                        Storage.ObjectNames.Add(entry, new ObjectName { ObjectType = ObjectType.Item, Name = item.Name }, packet.TimeSpan);
                        packet.AddSniffData(StoreNameType.Item, (int)entry, "DB_REPLY");
                        break;
                    }
                case DB2Hash.KeyChain:
                    {
                        db2File.ReadUInt32("Key Chain Id");
                        db2File.WriteLine("Key: {0}", Utilities.ByteArrayToHexString(db2File.ReadBytes(32)));
                        break;
                    }
                case DB2Hash.SceneScript: // lua ftw!
                    {
                        db2File.ReadUInt32("Scene Script Id");
                        if (db2File.ReadUInt16() > 0)
                            db2File.ReadCString("Name");

                        if (db2File.ReadUInt16() > 0)
                            db2File.ReadCString("Script");
                        // note they act as a kind of script "relations"; may not be exactly that.
                        db2File.ReadUInt32("Previous Scene Script");
                        db2File.ReadUInt32("Next Scene Script");
                        break;
                    }
                case DB2Hash.Vignette:
                    {
                        db2File.ReadUInt32("Vignette Entry");
                        if (db2File.ReadUInt16() > 0)
                            db2File.ReadCString("Name");

                        db2File.ReadUInt32("Icon");
                        db2File.ReadUInt32("Flag"); // not 100% sure (8 & 32 as values only) - todo verify with more data
                        db2File.ReadSingle("Unk Float 1");
                        db2File.ReadSingle("Unk Float 2");
                        break;
                    }
                case DB2Hash.WbAccessControlList:
                    {
                        db2File.ReadUInt32("Id");

                        if (db2File.ReadUInt16() > 0)
                            db2File.ReadCString("Address");

                        db2File.ReadUInt32("UnkMoP1");
                        db2File.ReadUInt32("UnkMoP1");
                        db2File.ReadUInt32("UnkMoP1");
                        db2File.ReadUInt32("UnkMoP1"); // flags?
                        break;
                    }
                default:
                    {
                        db2File.WriteLine("Unknown DB2 file type: {0} (0x{0:x})", type);
                        for (var i = 0; ; ++i)
                        {
                            if (db2File.Length - 4 >= db2File.Position)
                            {
                                var blockVal = db2File.ReadUpdateField();
                                string key = "Block Value " + i;
                                string value = blockVal.UInt32Value + "/" + blockVal.SingleValue;
                                packet.WriteLine(key + ": " + value);
                            }
                            else
                            {
                                var left = db2File.Length - db2File.Position;
                                for (var j = 0; j < left; ++j)
                                {
                                    string key = "Byte Value " + i;
                                    var value = db2File.ReadByte();
                                    packet.WriteLine(key + ": " + value);
                                }
                                break;
                            }
                        }
                        break;
                    }
            }
        }
    }
}
