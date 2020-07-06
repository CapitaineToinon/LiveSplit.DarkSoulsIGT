﻿using System;
using PropertyHook;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LiveSplit.ComponentUtil;
using System.Security.Cryptography;

namespace LiveSplit.DarkSoulsIGT
{
    class PrepareToDie : DarkSouls
    {
        /// <summary>
        /// Constants
        /// </summary>
        private readonly byte[] AES_KEY = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 };
        private const string CHR_DATA_AOB = "83 EC 14 A1 ? ? ? ? 8B 48 04 8B 40 08 53 55 56 57 89 4C 24 1C 89 44 24 20 3B C8";
        private const string CHAR_CLASS_BASE_AOB = "8B 0D ? ? ? ? 8B 7E 1C 8B 49 08 8B 46 20 81 C1 B8 01 00 00 57 51 32 DB";
        private const string CURRENT_SLOT_AOB = "8B 0D ? ? ? ? 80 B9 4F 0B 00 00 00 C6 44 24 28 00";
        private const string INVENTORY_RESET_AOB = "8B 4C 24 34 8B 44 24 2C 89 8A 38 01 00 00 8B 90 08 01 00 00 C1 E2 10 0B 90 00 01 00 00 8B C1 8B CD 89 14 AD ? ? ? ?";

        /// <summary>
        /// Constructor
        /// Needs to RescanAOB() for pointers to update
        /// </summary>
        /// <param name="process"></param>
        public PrepareToDie(PHook process) : base(process)
        {
            // Set pointers
            pCharClassBase = Process.RegisterAbsoluteAOB(CHAR_CLASS_BASE_AOB, 2, 0);
            pInventoryReset = Process.RegisterAbsoluteAOB(INVENTORY_RESET_AOB, 36);
            pLoaded = Process.RegisterAbsoluteAOB(CHR_DATA_AOB, 4, 0, 0x4, 0x0);
            pCurrentSlot = Process.RegisterAbsoluteAOB(CURRENT_SLOT_AOB, 2, 0);

            Process.RescanAOB();
        }

        /// <summary>
        /// Returns the raw IGT from Memory
        /// </summary>
        /// <returns></returns>
        public override int MemoryIGT
        {
            get => pCharClassBase.ReadInt32(0x68);
        }

        /// <summary>
        /// Current Save Slot index
        /// </summary>
        public override int CurrentSaveSlot
        {
            get => pCurrentSlot.ReadInt32(0xA70);
        }
        
        /// <summary>
        /// Current new game count
        /// </summary>
        public override int NgCount
        {
            get => pCharClassBase.ReadInt32(0x003C);
        }

        /// <summary>
        /// Returns the IGT of a specific slot in the game's savefile
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public override int GetCurrentSlotIGT(int slot = 0)
        {
            int igt = -1;

            var MyDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = Path.Combine(MyDocuments, "NBGI\\DarkSouls").ToString();
            var candidates = Directory.GetFiles(path, "DRAKS0005.sl2", SearchOption.AllDirectories);

            foreach (var candidate in candidates)
            {
                try
                {
                    byte[] file = File.ReadAllBytes(candidate);
                    int saveSlotSize = 0x60020;

                    // Seems like GFWL files have a different slot size
                    if (file.Length != 4326432)
                        saveSlotSize = 0x60190;

                    int igtOffset = 0x2dc + saveSlotSize * CurrentSaveSlot;
                    igt = BitConverter.ToInt32(file, igtOffset);
                    break;
                } catch
                {
                    // nothing, try the next candidate
                }
            }

            return igt;
        }
    }
}
