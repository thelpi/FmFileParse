﻿using CMScouterFunctions.DataClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CMScouterFunctions.Converters
{

    internal class NationConverter
    {
        private static readonly List<string> EUNations = new List<string>() {
            "AUSTRIA", "BELGIUM", "BULGARIA", "CROATIA", "CYRPUS", "CZECH REPUBLIC", "DENMARK", "ENGLAND", "ESTONIA", "SPAIN", "FINLAND", "FRANCE",
            "GERMANY", "GREECE", "HUNGARY", "REPUBLIC OF IRELAND", "ICELAND", "ITALY", "LATVIA", "LITHUANIA", "LUXEMBOURG", "MALTA", "NETHERLANDS", "NORTHERN IRELAND",
            "NORWAY", "POLAND", "PORTUGAL", "ROMANIA", "SCOTLAND", "SLOVAKIA", "SWITZERLAND", "SLOVENIA", "SWEDEN", "WALES"
        };

        public Nation Convert(byte[] source)
        {
            var nation = new Nation();

            nation.Id = ByteHandler.GetIntFromBytes(source, 0);
            nation.Name = ByteHandler.GetStringFromBytes(source, 4, 50);
            nation.EUNation = EUNations.Contains(nation.Name, StringComparer.InvariantCultureIgnoreCase);

            return nation;
        }
    }

    internal class StaffConverter
    {
        public Staff Convert(byte[] source)
        {
            var staff = new Staff();

            staff.StaffId = ByteHandler.GetIntFromBytes(source, 0);
            staff.StaffPlayerId = ByteHandler.GetIntFromBytes(source, 97);
            staff.FirstNameId = ByteHandler.GetIntFromBytes(source, 4);
            staff.SecondNameId = ByteHandler.GetIntFromBytes(source, 8);
            staff.CommonNameId = ByteHandler.GetIntFromBytes(source, 12);
            staff.DOB = ByteHandler.GetDateFromBytes(source, 16) ?? new DateTime(1985, 1, 1);
            staff.NationId = ByteHandler.GetIntFromBytes(source, 26);
            staff.SecondaryNationId = ByteHandler.GetIntFromBytes(source, 30);
            staff.InternationalCaps = ByteHandler.GetByteFromBytes(source, 24);
            staff.InternationalGoals = ByteHandler.GetByteFromBytes(source, 25);
            staff.ContractExpiryDate = ByteHandler.GetDateFromBytes(source, 70);
            staff.Wage = ByteHandler.GetIntFromBytes(source, 78);
            staff.Value = ByteHandler.GetIntFromBytes(source, 82);
            staff.ClubId = ByteHandler.GetIntFromBytes(source, 57);

            List<int> playerTests = new List<int>() { 11282, 35425, 70038, 21195, 120511, 16406 };
            if (playerTests.Contains(staff.StaffPlayerId))
            {
                var dobBytes = string.Join(", ", source.Skip(16).Take(5));
                var epiryBytes = string.Join(", ", source.Skip(70).Take(5));
            }
            
            staff.Adaptability = ByteHandler.GetByteFromBytes(source, 86);
            staff.Ambition = ByteHandler.GetByteFromBytes(source, 87);
            staff.Determination = ByteHandler.GetByteFromBytes(source, 88);
            staff.Loyalty = ByteHandler.GetByteFromBytes(source, 89);
            staff.Pressure = ByteHandler.GetByteFromBytes(source, 90);
            staff.Professionalism = ByteHandler.GetByteFromBytes(source, 91);
            staff.Sportsmanship = ByteHandler.GetByteFromBytes(source, 92);
            staff.Temperament = ByteHandler.GetByteFromBytes(source, 93);
            
            return staff;
        }
    }

    internal class ClubConverter
    {
        public Club Convert(byte[] source)
        {
            var club = new Club
            {
                ClubId = ByteHandler.GetIntFromBytes(source, 0),
                LongName = ByteHandler.GetStringFromBytes(source, 4, 50),
                Name = ByteHandler.GetStringFromBytes(source, 56, 25),
                NationId = ByteHandler.GetIntFromBytes(source, 83),
                DivisionId = ByteHandler.GetIntFromBytes(source, 87),
                Reputation = ByteHandler.GetShortFromBytes(source, 128)
            };
            return club;
        }
    }

    internal class ClubCompConverter
    {
        public Club_Comp Convert(byte[] source)
        {
            var comp = new Club_Comp();

            comp.Id = ByteHandler.GetIntFromBytes(source, 0);
            comp.LongName = ByteHandler.GetStringFromBytes(source, 4, 50);
            comp.Name = ByteHandler.GetStringFromBytes(source, 56, 25);
            //comp.Reputation = ByteHandler.GetByteFromBytes(source, 82);
            comp.NationId = ByteHandler.GetIntFromBytes(source, 92);
            comp.Abbreviation = ByteHandler.GetStringFromBytes(source, 83, 3);

            return comp;
        }
    }
    internal class ContractConverter
    {
        private decimal _valueMultiplier = 1;

        public ContractConverter(SaveGameData gameData)
        {
            _valueMultiplier = gameData.ValueMultiplier;
        }

        public Contract Convert(byte[] source)
        {
            var contract = new Contract();

            contract.PlayerId = ByteHandler.GetIntFromBytes(source, 0);
            contract.WagePerWeek = (int)(ByteHandler.GetIntFromBytes(source, 12) * _valueMultiplier);
            contract.GoalBonus = (int)(ByteHandler.GetIntFromBytes(source, 16) * _valueMultiplier);
            contract.AssistBonus = (int)(ByteHandler.GetIntFromBytes(source, 20) * _valueMultiplier);
            contract.NonPromotionReleaseClause = ByteHandler.GetByteFromBytes(source, 28) == 1;
            contract.MinimumFeeReleaseClause = ByteHandler.GetByteFromBytes(source, 29) == 1;
            contract.NonPlayingReleaseClause = ByteHandler.GetByteFromBytes(source, 30) == 1;
            contract.RelegationReleaseClause = ByteHandler.GetByteFromBytes(source, 31) == 1;
            contract.ManagerReleaseClause = ByteHandler.GetByteFromBytes(source, 32) == 1;
            contract.ReleaseClauseValue = (int)(ByteHandler.GetIntFromBytes(source, 33) * _valueMultiplier);
            contract.ContractStartDate = ByteHandler.GetDateFromBytes(source, 37);
            contract.ContractEndDate = ByteHandler.GetDateFromBytes(source, 45);
            contract.TransferStatus = ByteHandler.GetByteFromBytes(source, 78);
            contract.SquadStatus = ByteHandler.GetByteFromBytes(source, 79);

            return contract;
        }
    }

    internal class PlayerDataConverter
    {
        public PlayerData Convert(byte[] source)
        {
            var player = new PlayerData();

            player.PlayerId = ByteHandler.GetIntFromBytes(source, 0);
            player.CurrentAbility = ByteHandler.GetShortFromBytes(source, 5);
            player.PotentialAbility = ByteHandler.GetShortFromBytes(source, 7);
            player.Reputation = ByteHandler.GetShortFromBytes(source, 9);
            player.DomesticReputation = ByteHandler.GetShortFromBytes(source, 11);
            player.WorldReputation = ByteHandler.GetShortFromBytes(source, 13);
            player.GK = ByteHandler.GetByteFromBytes(source, 15);
            player.SW = ByteHandler.GetByteFromBytes(source, 16);
            player.DF = ByteHandler.GetByteFromBytes(source, 17);
            player.DM = ByteHandler.GetByteFromBytes(source, 18);
            player.MF = ByteHandler.GetByteFromBytes(source, 19);
            player.AM = ByteHandler.GetByteFromBytes(source, 20);
            player.ST = ByteHandler.GetByteFromBytes(source, 21);
            player.WingBack = ByteHandler.GetByteFromBytes(source, 22);
            player.Left = ByteHandler.GetByteFromBytes(source, 24);
            player.Right = ByteHandler.GetByteFromBytes(source, 23);
            player.Centre = ByteHandler.GetByteFromBytes(source, 25);
            player.FreeRole = ByteHandler.GetByteFromBytes(source, 26);
            player.Acceleration = ByteHandler.GetByteFromBytes(source, 27);
            player.Aggression = ByteHandler.GetByteFromBytes(source, 28);
            player.Agility = ByteHandler.GetByteFromBytes(source, 29);
            player.Anticipation = ByteHandler.GetByteFromBytes(source, 30, true);
            player.Balance = ByteHandler.GetByteFromBytes(source, 31);
            player.Bravery = ByteHandler.GetByteFromBytes(source, 32);
            player.Consistency = ByteHandler.GetByteFromBytes(source, 33);
            player.Corners = ByteHandler.GetByteFromBytes(source, 34);
            player.Creativity = ByteHandler.GetByteFromBytes(source, 67, true);
            player.Crossing = ByteHandler.GetByteFromBytes(source, 35, true);
            player.Decisions = ByteHandler.GetByteFromBytes(source, 36, true);
            player.Dirtiness = ByteHandler.GetByteFromBytes(source, 37);
            player.Dribbling = ByteHandler.GetByteFromBytes(source, 38, true);
            player.Finishing = ByteHandler.GetByteFromBytes(source, 39, true);
            player.Flair = ByteHandler.GetByteFromBytes(source, 40);
            player.FreeKicks = ByteHandler.GetByteFromBytes(source, 41);
            player.Handling = ByteHandler.GetByteFromBytes(source, 42, true);
            player.Heading = ByteHandler.GetByteFromBytes(source, 43, true);
            player.ImportantMatches = ByteHandler.GetByteFromBytes(source, 44);
            player.Influence = ByteHandler.GetByteFromBytes(source, 47);
            player.InjuryProneness = ByteHandler.GetByteFromBytes(source, 45);
            player.Jumping = ByteHandler.GetByteFromBytes(source, 46);
            player.LongShots = ByteHandler.GetByteFromBytes(source, 49, true);
            player.Marking = ByteHandler.GetByteFromBytes(source, 50, true);
            player.NaturalFitness = ByteHandler.GetByteFromBytes(source, 52);
            player.OffTheBall = ByteHandler.GetByteFromBytes(source, 51, true);
            player.OneOnOnes = ByteHandler.GetByteFromBytes(source, 53, true);
            player.Pace = ByteHandler.GetByteFromBytes(source, 54);
            player.Passing = ByteHandler.GetByteFromBytes(source, 55, true);
            player.Penalties = ByteHandler.GetByteFromBytes(source, 56, true);
            player.Positioning = ByteHandler.GetByteFromBytes(source, 57, true);
            player.Reflexes = ByteHandler.GetByteFromBytes(source, 58, true);
            player.Stamina = ByteHandler.GetByteFromBytes(source, 60);
            player.Strength = ByteHandler.GetByteFromBytes(source, 61);
            player.Tackling = ByteHandler.GetByteFromBytes(source, 62, true);
            player.Teamwork = ByteHandler.GetByteFromBytes(source, 63);
            player.Technique = ByteHandler.GetByteFromBytes(source, 64);
            player.ThrowIns = ByteHandler.GetByteFromBytes(source, 65, true);
            player.Versatility = ByteHandler.GetByteFromBytes(source, 66);
            player.WorkRate = ByteHandler.GetByteFromBytes(source, 68);

            player.LeftFoot = ByteHandler.GetByteFromBytes(source, 48);
            player.RightFoot = ByteHandler.GetByteFromBytes(source, 59);

            return player;
        }

        public byte GetIntrinsicMask(byte val)
        {
            if (val < 32) return 1;
            if (val < 64) return 2;
            if (val < 72) return 3;
            if (val < 80) return 4;
            if (val < 87) return 5;
            if (val < 92) return 6;
            if (val < 96) return 7;

            if (val < 99) return 8;
            if (val < 102) return 9;
            if (val < 107) return 10;

            if (val < 111) return 11;
            if (val < 114) return 12;
            if (val < 119) return 13;

            if (val < 123) return 14;

            if (val < 127) return 15;
            if (val < 130) return 16;
            if (val < 134) return 17;

            if (val < 139) return 18;
            if (val < 146) return 19;
            if (val < 155) return 20;

            if (val < 170) return 21;
            if (val < 185) return 22;

            return 23;
        }

        public byte GetIntrinsicMask2(byte val)
        {
            if (val < 32) return 1;
            if (val < 50) return 2;
            if (val < 65) return 3;
            if (val < 81) return 4;
            if (val < 88) return 5;
            if (val < 94) return 6;

            if (val < 99) return 7;
            if (val < 104) return 8;
            if (val < 107) return 9;

            if (val < 108) return 10;
            if (val < 109) return 11;
            if (val < 110) return 12;

            if (val < 111) return 13;

            if (val < 114) return 14;
            if (val < 118) return 15;
            if (val < 122) return 16;

            if (val < 128) return 17;
            if (val < 135) return 18;
            if (val < 150) return 19;

            return 20;
        }
    }
}
