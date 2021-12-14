using UnityEngine;
using System; 
using System.Collections;
using System.Collections.Generic;
using System.Xml;

/// <summary>
/// A structure of abilities that can be placed on a card.
/// </summary>
/// 
/// mAbilityList: List of all the AbilityChunks that make up the ability. 
/// mName: Name of the ability, which draws the data from the database. 
/// mCost: Cost of the ability in points.
/// 
/// mTimesAvailable: Number of times the ability can be used in a single turn. 
/// mTimesLeft: Number of times the ability has remaining this turn, which is reset to mTimesAvailable at the end of each turn.
/// 
/// mHolderReliant: Is this deleted if the ability holder is dead?
/// mHolder: Who's the holder of this ability?
/// mEndsTurn: Does this ability end the user's turn when activated?
/// 
/// mCooldown: How many turns has to pass before this ability is available again?
/// mCooldownRemaining: How many turns remain before this ability is available again? The ability is useable at 0 turns. 
/// 
/// mTrigger: When does this ability automatically trigger? Use Instant to force it to only trigger on use. 

public class AbilityBlock {
    public List<AbilityChunk> mAbilityList = new List<AbilityChunk>();
    public string mName {
        get;
        private set;
    }
    public string mDisplayName;
    public string mNote;
    public string mIdentifier;
    ushort mID;
    public string mDescription {
        get;
        private set;
    }
    public int mCost {
        get;
        set;
    }

    string fail;

    public int mXPCost;

    // if timesAvailable == -1, infinite uses; reset timesLeft to timesAvailable at end of turn
    public int mTimesAvailable {
        get;
        private set;
    }
    public int mTimesLeft { 
        get;
        set;
    }
    
    // times useable
    public bool mLimitedUse {
        get;
        private set;
    }
    
    //public bool mIsHolderReliant { //gets removed if the ability holder died
    //    get;
    //    private set;
    //}
    public ActivateArea mActivateFrom;

    public Card mHolder;
    public Card mTriggerCard;
    public bool mEndsTurn { //does this ability end the user's turn when activated?
        get;
        private set;
    }
    public int mCooldown {
        get;
        private set;
    }
    public int mCooldownRemaining;

    /// <summary>
    /// When the ability triggers; use Instant to force it to trigger on use only.
    /// </summary>
    public TriggerTime mTrigger {
        get;
        private set;
    }

    public int mCode;
    public int mDelayed;
    public int mDelayRemaining;
    public AffectedZone mZone;
    public int mGlobalTimes;
    public DeathMethod mDeathMethod;
    public TargetType mTriggerCardType;
    public string mCondition;
    public int mConditionDefine;
    public bool mPriorityAbility;
    public string mMessage;
    public List<string> mTags;

    public OptimalUse mOptimal;
    public bool delayedAbility;

    // =======================

    public AbilityBlock(Card pHolder) {
        mHolder = pHolder; 
    }

    public AbilityBlock()
    {
        mAbilityList = new List<AbilityChunk>();
    }

    public AbilityBlock(string pName) { InitAbilityBlock(pName, GameManager.mSingleton.turnPlayer.deck.CardBack, true, false, false); }
    public AbilityBlock(string pName, Card pHolder, bool ability) { InitAbilityBlock(pName, pHolder, ability, false, false); }
    public AbilityBlock(string pName, Card pHolder, bool ability, bool fast) { InitAbilityBlock(pName, pHolder, ability, fast, false); }

    void InitAbilityBlock(string pName, Card pHolder, bool ability, bool fast, bool group)
    {
        XmlDocument doc = new XmlDocument();
        TextAsset textAsset;
        XmlNode pNode;

        if (ability)
        {
            if (!XMLManager.loaded)
            {
                textAsset = (TextAsset)Resources.Load("XML/AbilitiesNew");
                doc.LoadXml(textAsset.text);
            }
            else
                doc = XMLManager.ab_a;
            pNode = doc.SelectSingleNode("Abilities/" + pName);
        }
        else
        {
            if (!XMLManager.loaded)
            {
                textAsset = (TextAsset)Resources.Load("XML/Upgrades");
                doc.LoadXml(textAsset.text);
            }
            else
                doc = XMLManager.ab_u;
            pNode = doc.SelectSingleNode("Upgrades/" + pName);
        }

        if (pNode == null) { GameLog.Write(pName + " ability was not found."); return; }

        int pTempCost, pTempXPCost, pTempTimes, pTempCooldown, pTempDelayed, pTempGlobalTimes;
        ushort pTempID;
        bool pTempEndsTurn, pTempLimit, pTempPriorityAbility;

        mIdentifier = pName;
        mID = (UInt16.TryParse(pNode.GetText("ID"), out pTempID)) ? pTempID : (ushort)0;
        mName = (pNode.GetText("Name") != null) ? pNode["Name"].InnerText : "";
        mDisplayName = (pNode.GetText("DisplayName") != null) ? pNode["DisplayName"].InnerText : mName;
        mNote = (pNode.GetText("Note") != null) ? pNode["Note"].InnerText : "";
        mDescription = (pNode.GetText("Description") != null) ? pNode["Description"].InnerText : "";
        mCost = (Int32.TryParse(pNode.GetText("Cost"), out pTempCost)) ? pTempCost : 0;
        mTimesAvailable = (Int32.TryParse(pNode.GetText("Times"), out pTempTimes)) ? pTempTimes : -1;
        mCooldown = (Int32.TryParse(pNode.GetText("Cooldown"), out pTempCooldown)) ? pTempCooldown : 0;
        mDelayed = (Int32.TryParse(pNode.GetText("Delay"), out pTempDelayed)) ? pTempDelayed : 0;
        if (mDelayed > 0) delayedAbility = true;
        mGlobalTimes = (Int32.TryParse(pNode.GetText("GlobalTimes"), out pTempGlobalTimes)) ? pTempGlobalTimes : -1;
        mLimitedUse = (bool.TryParse(pNode.GetText("Limited"), out pTempLimit)) ? pTempLimit : false;

        string unparsedTags = (pNode.GetText("Tag") != null) ? pNode["Tag"].InnerText : "";
        mTags = new List<string>(unparsedTags.Split('\t'));

        //IMPORTANT, if we want to fast load, only information relevant to display the card is loaded, other stuff is ignored
        if (fast) { return; }
        //-----------------

        mXPCost = (Int32.TryParse(pNode.GetText("XPCost"), out pTempXPCost)) ? pTempXPCost : 0;
        mTrigger = (pNode.GetText("Trigger") != null) ? (TriggerTime)Enum.Parse(typeof(TriggerTime), pNode["Trigger"].InnerText) : TriggerTime.Instant;
        mActivateFrom = (pNode.GetText("ActivateFrom") != null) ? (ActivateArea)Enum.Parse(typeof(ActivateArea), pNode["ActivateFrom"].InnerText) : ActivateArea.Map;
        mEndsTurn = (bool.TryParse(pNode.GetText("EndsTurn"), out pTempEndsTurn)) ? pTempEndsTurn : false;
        mZone = (pNode.GetText("Zone") != null) ? (AffectedZone)Enum.Parse(typeof(AffectedZone), pNode["Zone"].InnerText) : AffectedZone.Both;
        mDeathMethod = (pNode.GetText("DeathMethod") != null) ? (DeathMethod)Enum.Parse(typeof(DeathMethod), pNode["DeathMethod"].InnerText) : DeathMethod.All;
        mTriggerCardType = (pNode.GetText("TriggerCardType") != null) ? (TargetType)Enum.Parse(typeof(TargetType), pNode["TriggerCardType"].InnerText) : TargetType.All;
        mCondition = (pNode.GetText("Condition") != null) ? pNode["Condition"].InnerText : "";
        mConditionDefine = (pNode.GetText("ConditionDefine") != null) ? int.Parse(pNode["ConditionDefine"].InnerText) : 0;
        mPriorityAbility = (pNode.GetText("PriorityAbility") != null) ? bool.Parse(pNode["PriorityAbility"].InnerText) : false;
        mMessage = (pNode.GetText("Message") != null) ? pNode["Message"].InnerText : "";

        mTriggerCard = new Card();
        mOptimal = (pNode.GetText("OptimalUse") != null) ? (OptimalUse)Enum.Parse(typeof(OptimalUse), pNode["OptimalUse"].InnerText) : OptimalUse.None;

        //set limited use to true if we are running one of these types of abilities, since we will never want the value to reset on turn start
        if (mTrigger == TriggerTime.Continuous || mTrigger == TriggerTime.End || mTrigger == TriggerTime.Start || mTrigger == TriggerTime.Transition)
            mLimitedUse = true;

        // default numbers
        mTimesLeft = mTimesAvailable;
        mCooldownRemaining = 0;

        mHolder = pHolder;

        XmlNodeList pAbilityChunks = pNode.SelectNodes("Chunk");
        foreach (XmlNode pChunkNode in pAbilityChunks)
        {
            // gets written values if provided, otherwise uses default values
            TriggerTime pTrigger = (pChunkNode.GetText("TriggerTime")!= null) ? (TriggerTime)Enum.Parse(typeof(TriggerTime), pChunkNode["TriggerTime"].InnerText) : TriggerTime.Instant;
            int pNumTargets = (pChunkNode.GetText("NumTargets") != null) ? int.Parse(pChunkNode["NumTargets"].InnerText) : 0;
            TargetType pTargetType = (pChunkNode.GetText("TargetType") != null) ? (TargetType)Enum.Parse(typeof(TargetType), pChunkNode["TargetType"].InnerText) : TargetType.All;
            TargetPlayer pTargetPlayer = (pChunkNode.GetText("TargetPlayer") != null) ? (TargetPlayer)Enum.Parse(typeof(TargetPlayer), pChunkNode["TargetPlayer"].InnerText) : TargetPlayer.AllAllies;
            int pRange = (pChunkNode.GetText("Range") != null) ? int.Parse(pChunkNode["Range"].InnerText) : 0;
            EffectType pEffect = (pChunkNode.GetText("Effect") != null) ? (EffectType)Enum.Parse(typeof(EffectType), pChunkNode["Effect"].InnerText) : EffectType.HPMod;
            int pStackNum = (pChunkNode.GetText("StackNum") != null) ? int.Parse(pChunkNode["StackNum"].InnerText) : -1;
            float pPrimary = (pChunkNode.GetText("PrimaryValue") != null) ? float.Parse(pChunkNode["PrimaryValue"].InnerText) : 0;
            string pSecondary = (pChunkNode.GetText("SecondaryValue") != null) ? pChunkNode["SecondaryValue"].InnerText : "";
            int pTertiary = (pChunkNode.GetText("TertiaryValue") != null) ? int.Parse(pChunkNode["TertiaryValue"].InnerText) : 0;
            bool pAffectsCommander = (pChunkNode.GetText("AffectsCommander") != null) ? bool.Parse(pChunkNode["AffectsCommander"].InnerText) : false;
            int pTimesAvailable = (pChunkNode.GetText("Times") != null) ? int.Parse(pChunkNode["Times"].InnerText) : -1;
            bool pLimitedUse = (pChunkNode.GetText("Limited") != null) ? bool.Parse(pChunkNode["Limited"].InnerText) : false;
            int pCost = (pChunkNode.GetText("Cost") != null) ? int.Parse(pChunkNode["Cost"].InnerText) : 0;
            bool pHolderReliant = (pChunkNode.GetText("HolderReliant") != null) ? bool.Parse(pChunkNode["HolderReliant"].InnerText) : true;
            bool pOwnerAdjacent = (pChunkNode.GetText("OwnerAdjacent") != null) ? bool.Parse(pChunkNode["OwnerAdjacent"].InnerText) : false;
            AffectedZone pZone = (pChunkNode.GetText("Zone") != null) ? (AffectedZone)Enum.Parse(typeof(AffectedZone), pChunkNode["Zone"].InnerText) : AffectedZone.Both;
            bool pAffectsBaseStats = (pChunkNode.GetText("AffectsBaseStats") != null) ? bool.Parse(pChunkNode["AffectsBaseStats"].InnerText) : false;
            float pLimitingFactor = (pChunkNode.GetText("LimitingFactor") != null) ? float.Parse(pChunkNode["LimitingFactor"].InnerText) : -1;
            int pLimitingNumber = (pChunkNode.GetText("LimitingNumber") != null) ? int.Parse(pChunkNode["LimitingNumber"].InnerText) : -1;
            bool pRangeFromTrigger = (pChunkNode.GetText("RangeFromTrigger") != null) ? bool.Parse(pChunkNode["RangeFromTrigger"].InnerText) : false;
            bool pRangeFromTarget = (pChunkNode.GetText("RangeFromTarget") != null) ? bool.Parse(pChunkNode["RangeFromTarget"].InnerText) : false;
            bool pRangeFromSpecificHex = (pChunkNode.GetText("RangeFromSpecificHex") != null) ? bool.Parse(pChunkNode["RangeFromSpecificHex"].InnerText) : false;
            SpawnLocation pSpawnLocaion = (pChunkNode.GetText("SpawnLocation") != null) ? (SpawnLocation)Enum.Parse(typeof(SpawnLocation), pChunkNode["SpawnLocation"].InnerText) : SpawnLocation.Holder;
            bool pStoreTargetInfo = (pChunkNode.GetText("StoreTargetInfo") != null) ? bool.Parse(pChunkNode["StoreTargetInfo"].InnerText) : false;
            bool pStoreNewCard = (pChunkNode.GetText("StoreNewCard") != null) ? bool.Parse(pChunkNode["StoreNewCard"].InnerText) : false;
            bool pUnmovedTarget = (pChunkNode.GetText("UnmovedTarget") != null) ? bool.Parse(pChunkNode["UnmovedTarget"].InnerText) : false;
            AffectedZone pHexZone = (pChunkNode.GetText("HexZone") != null) ? (AffectedZone)Enum.Parse(typeof(AffectedZone), pChunkNode["HexZone"].InnerText) : AffectedZone.Both;
            bool pRemoveCard = (pChunkNode.GetText("RemoveCard") != null) ? bool.Parse(pChunkNode["RemoveCard"].InnerText) : false;
            int pTargetStatMin = (pChunkNode.GetText("TargetStatMin") != null) ? int.Parse(pChunkNode["TargetStatMin"].InnerText) : 0;
            int pTargetStatMax = (pChunkNode.GetText("TargetStatMax") != null) ? int.Parse(pChunkNode["TargetStatMax"].InnerText) : 99;
            Stats pTargetStat = (pChunkNode.GetText("TargetStat") != null) ? (Stats)Enum.Parse(typeof(Stats), pChunkNode["TargetStat"].InnerText) : Stats.Cost;
            int pXPCost = (pChunkNode.GetText("XPCost") != null) ? int.Parse(pChunkNode["XPCost"].InnerText) : 0;
            string pCondition = (pChunkNode.GetText("Condition") != null) ? pChunkNode["Condition"].InnerText : "";
            int pConditionDefine = (pChunkNode.GetText("ConditionDefine") != null) ? int.Parse(pChunkNode["ConditionDefine"].InnerText) : 0;
            bool pOwnerExcluded = (pChunkNode.GetText("OwnerExcluded") != null) ? bool.Parse(pChunkNode["OwnerExcluded"].InnerText) : false;
            bool pStoreDamage = (pChunkNode.GetText("StoreDamage") != null) ? bool.Parse(pChunkNode["StoreDamage"].InnerText) : false;
            bool pGlobalStack = (pChunkNode.GetText("GlobalStack") != null) ? bool.Parse(pChunkNode["GlobalStack"].InnerText) : false;
            bool pCardStack = (pChunkNode.GetText("CardStack") != null) ? bool.Parse(pChunkNode["CardStack"].InnerText) : false;
            StoreType pStoreString = (pChunkNode.GetText("StoreValue") != null) ? (StoreType)Enum.Parse(typeof(StoreType), pChunkNode["StoreValue"].InnerText) : StoreType.None;
            string pSpecificTarget = (pChunkNode.GetText("SpecificTarget") != null) ? pChunkNode["SpecificTarget"].InnerText : "";
            string pMessage = (pChunkNode.GetText("Message") != null) ? pChunkNode["Message"].InnerText : "";
            bool pIgnoreNegation = (pChunkNode.GetText("IgnoreNegation") != null) ? bool.Parse(pChunkNode["IgnoreNegation"].InnerText) : false;
            VisualEffect pVisualEffect = (pChunkNode.GetText("VisualEffect") != null) ? (VisualEffect)Enum.Parse(typeof(VisualEffect), pChunkNode["VisualEffect"].InnerText) : VisualEffect.None;
            TargetClass pTargetClass = (pChunkNode.GetText("TargetClass") != null) ? (TargetClass)Enum.Parse(typeof(TargetClass), pChunkNode["TargetClass"].InnerText) : TargetClass.Both;
            Faction pTargetFaction = (pChunkNode.GetText("TargetFaction") != null) ? (Faction)Enum.Parse(typeof(Faction), pChunkNode["TargetFaction"].InnerText) : Faction.All;
            Faction pExcludedFaction = (pChunkNode.GetText("ExcludedFaction") != null) ? (Faction)Enum.Parse(typeof(Faction), pChunkNode["ExcludedFaction"].InnerText) : Faction.None;

            //parse target location and convert it to short
            string tempTargetLocation = (pChunkNode.GetText("TargetLocation") != null) ? pChunkNode["TargetLocation"].InnerText : "Map";
            short pTargetLocation;
            switch(tempTargetLocation)
            {
                default: case "Map": pTargetLocation = TargetLocation.Map; break;
                case "Hand": pTargetLocation = TargetLocation.Hand; break;
                case "SecondHand": pTargetLocation = TargetLocation.SecondHand; break;
                case "Commander": pTargetLocation = TargetLocation.Commander; break;
                case "Discard": pTargetLocation = TargetLocation.Discard; break;
                case "Deck": pTargetLocation = TargetLocation.Deck; break;
                case "Hex": pTargetLocation = TargetLocation.Hex; break;
                case "None": pTargetLocation = TargetLocation.None; break;
                case "All": pTargetLocation = TargetLocation.All; break;
                case "Other": pTargetLocation = TargetLocation.Other; break;
                case "Rune": pTargetLocation = TargetLocation.Rune; break;
                case "AllRunes": pTargetLocation = TargetLocation.AllRunes; break;
            }

            string unparsedSpecificHexes = (pChunkNode.GetText("SpecificHexes") != null) ? pChunkNode["SpecificHexes"].InnerText : "";
            List<int> pSpecificHexes = mHolder.getHexList(unparsedSpecificHexes);

            //set limited use to true if we are running one of these types of abilities, since we will never want the value to reset on turn start
            if (pTrigger == TriggerTime.Continuous || pTrigger == TriggerTime.End || pTrigger == TriggerTime.Start || pTrigger == TriggerTime.Transition)
                pLimitedUse = true;

            mAbilityList.Add(new AbilityChunk(mID, mName, pTrigger, pNumTargets, pTargetType, pTargetPlayer, pRange, pEffect, pStackNum, pPrimary, pSecondary, pTertiary, pAffectsCommander
                ,pTimesAvailable, pLimitedUse, pCost, pHolderReliant,pOwnerAdjacent, pZone, pAffectsBaseStats, pLimitingFactor, pLimitingNumber, pTargetLocation,
                pRangeFromTrigger, pSpawnLocaion, pStoreTargetInfo, pUnmovedTarget, pStoreNewCard, pHexZone, pRemoveCard, pTargetStatMin, pTargetStatMax, pTargetStat, 
                pXPCost, pCondition, pConditionDefine, pOwnerExcluded, pStoreDamage, mTrigger, pGlobalStack, pSpecificHexes, pStoreString, pSpecificTarget, pMessage, pIgnoreNegation,
                pRangeFromTarget, pVisualEffect, pTargetClass, pTargetFaction, pExcludedFaction, pCardStack, mPriorityAbility, pRangeFromSpecificHex
                ,mHolder/*, this*/)); 
        }

        fail = "";
    }

    /// <summary>
    /// Checks to see if this ability is available for use.  
    /// </summary>
    /// <returns>True if ability is available, false otherwise.</returns>
    public bool IsAvailable() {
        if (mCooldownRemaining <= 0) return true;
        else return false;
    }

    public void SetHolder(Card pHolder)
    {
        mHolder = pHolder;
        for(int i = 0; i < mAbilityList.Count; i++)
        {
            mAbilityList[i].mHolder = pHolder;
        }
    }

    /// <summary>
    /// Checks to see if this ability has any valid targets, if it can be executed or not
    /// Essentially this function uses the targeting rules to break down and check the location where the targets should be, and returns true if it finds one
    /// </summary>
    /// <returns></returns>
    public bool CanBeExecuted(Player p, Player e)
    {
        //first check cooldown and times remaining
        if (mCooldownRemaining != 0) {
            GameLog.Display(mDisplayName + " is on cooldown", false, true, true);
            return false;}
        else if (mTimesLeft == 0) {
            GameLog.Display(mDisplayName + " has no uses left", false, true, true);
            return false;}
        else if(!checkGlobalTimes())
        {
            GameLog.Display(mDisplayName + " has no uses left", false, true, true); return false;
        }
        else if(!checkCondition(p) && mTrigger != TriggerTime.Continuous)
        {
            GameLog.Display(mDisplayName + " condition failed: " + fail, false, true, true);
            fail = "";
            return false;
        }

        //check EACH chunk, and check the targeting rules for those chunks
        //if any of them fail, the ability cannot be executed
        for (int i = 0; i < mAbilityList.Count; i++)
        {
            if(mAbilityList[i].mNumTargets > 0) //if this chunk has a set number of targets
                //OR we have a bool defining that this chunk gets a free pass
            {
                //set the correct player target
                bool twice = false;
                Player pCheckingPlayer;
                if (mAbilityList[i].mTargetPlayer == TargetPlayer.All)
                {
                    pCheckingPlayer = p;
                    twice = true;
                }
                else if (mAbilityList[i].mTargetPlayer == TargetPlayer.AllAllies)
                    pCheckingPlayer = p;
                else
                    pCheckingPlayer = e;
                //---------------------------

            CheckTargets:

                List<Card> cards = new List<Card>();
                bool run_all_cases = false;

                switch(mAbilityList[i].mTargetLocation)
                {
                    case TargetLocation.Hex: //hex targets will always exist
                        continue;
                    case TargetLocation.AllRunes:
                        continue;
                    case TargetLocation.All: //run all NORMAL cases. Special cases should be left above this
                        run_all_cases = true;
                        goto case TargetLocation.Map;
                    case TargetLocation.Map:
                        for (int x = 0; x < pCheckingPlayer.unit_mgr.all_units.Count; x++) { //for all units
                            //check range
                            if((!mAbilityList[i].mOwnerAdjacent || MapManager.inRange(0, mAbilityList[i].mRange, mHolder.mCurHexes, pCheckingPlayer.unit_mgr.all_units[x].getHex()))
                                && mAbilityList[i].checkZone(mHolder.is_P1, pCheckingPlayer.unit_mgr.all_units[x].getHex())
                                && (!mAbilityList[i].mOwnerExcluded || mAbilityList[i].mCode != pCheckingPlayer.unit_mgr.all_units[x].code))
                                cards.Add(pCheckingPlayer.unit_mgr.all_units[x].myCard); }
                        for (int x = 0; x < pCheckingPlayer.unit_mgr.all_structures.Count; x++) { //for all structures
                            //check range
                         //   GameLog.Write("Is checking player's " + pCheckingPlayer.unit_mgr.all_structures[x].name + " in range of " +
                           //     mHolder.mCurHexes[0].ToString() + "?");
                            if ((!mAbilityList[i].mOwnerAdjacent || MapManager.inRange(0, mAbilityList[i].mRange, mHolder.mCurHexes, pCheckingPlayer.unit_mgr.all_structures[x].getHexes()))
                                && mAbilityList[i].checkZone(mHolder.is_P1, pCheckingPlayer.unit_mgr.all_structures[x].getHexes()[0])
                                && (!mAbilityList[i].mOwnerExcluded || mAbilityList[i].mCode != pCheckingPlayer.unit_mgr.all_structures[x].code))
                                cards.Add(pCheckingPlayer.unit_mgr.all_structures[x].myCard); }
                        if (CheckCardType(cards, mAbilityList[i].mTargetType, mAbilityList[i].mTargetClass, mAbilityList[i].mTargetFaction, mAbilityList[i].mExcludedFaction) //check type of card
                            ) continue;
                        else if (run_all_cases) goto case TargetLocation.Commander;                        
                        else
                        {
                            if (!twice)
                            {
                                if(!mHolder.negated) GameLog.Display(mDisplayName + " found no valid targets", false, true, true);
                                GameLog.Write(mDisplayName + " found no valid targets");
                                return false;
                            }
                        }
                        break;
                    case TargetLocation.Commander:
                        if (!pCheckingPlayer.commanderFielded()) continue; //commander is a valid target if it is not fielded
                        else if(run_all_cases) goto case TargetLocation.Deck;
                        else if (!twice) return false;
                        break;
                    case TargetLocation.Deck:
                        cards.AddRange(pCheckingPlayer.deck.myDeck);
                         if (CheckCardType(cards, mAbilityList[i].mTargetType, mAbilityList[i].mTargetClass, mAbilityList[i].mTargetFaction, mAbilityList[i].mExcludedFaction))
                            continue; //check type of card
                        else if (run_all_cases) goto case TargetLocation.Hand;
                        else if (!twice) return false;
                        break;
                    case TargetLocation.Hand:
                        cards.AddRange(pCheckingPlayer.deck.Hand);
                        if (CheckCardType(cards, mAbilityList[i].mTargetType, mAbilityList[i].mTargetClass, mAbilityList[i].mTargetFaction, mAbilityList[i].mExcludedFaction))
                            continue; //check type of card
                        else if (run_all_cases) goto case TargetLocation.SecondHand;
                        else if (!twice) return false;
                        break;
                    case TargetLocation.SecondHand:
                        cards.AddRange(pCheckingPlayer.deck.SecondHand);
                        if (CheckCardType(cards, mAbilityList[i].mTargetType, mAbilityList[i].mTargetClass, mAbilityList[i].mTargetFaction, mAbilityList[i].mExcludedFaction))
                            continue; //check type of card
                        else if (run_all_cases) goto case TargetLocation.Discard;
                        else if (!twice) return false;
                        break;
                    case TargetLocation.Discard:
                        cards.AddRange(pCheckingPlayer.deck.Discards);
                        if (CheckCardType(cards, mAbilityList[i].mTargetType, mAbilityList[i].mTargetClass, mAbilityList[i].mTargetFaction, mAbilityList[i].mExcludedFaction))
                            continue; //check type of card
                        else if (!twice) return false;
                        break;
                }

                //if we were targeting both players, run the target checks again for the other player
                if (twice)
                {
                    twice = false;
                    pCheckingPlayer = e;
                    goto CheckTargets;
                }
                //----------------------

            }
            //Abilities that have zero targets or target all, can be executed even if there are no valid targets
            else
            {
                continue;
            }
        } //end chunk for loop
        //GameLog.Time(mName + " cannot be executed");
            return true;
    }

    /// <summary>
    /// used in CanBeExecuted to see if the card type (among other things) is valid
    /// returns true if we found a valid target
    /// </summary>
    /// <returns></returns>
    bool CheckCardType(List<Card> cards, TargetType type, TargetClass targetClass, Faction targetFaction, Faction excludedFaction)
    {
      //  Debug.Log("Check card type");
        //All, Unit, Structure, or Instant
        for(int i = 0; i < cards.Count; i++)
        {
            //check if it is a minion
            //if we cannot target minions, but this card is a minion, skip it
            if (targetClass == TargetClass.NonMinion && (cards[i].type == 'm' || cards[i].type == 'n')) continue;
            else if (targetClass == TargetClass.Minion && (cards[i].type != 'm' && cards[i].type != 'n')) continue;

            //Check valid factions
            //if target faction is not all, and the card does not have that faction, skip this card
            if (targetFaction != Faction.All && !cards[i].HasFaction(targetFaction)) continue;
            //if excluded faction is not none, and the card has that faction, skip this card
            else if (excludedFaction != Faction.None && cards[i].HasFaction(excludedFaction)) continue;

            switch(type)
            {
                case TargetType.TargetCard:
                case TargetType.TriggerCard:
                case TargetType.Self:
                case TargetType.All:
                    return true;
                case TargetType.Unit:
                    if (cards[i].type == 'u' || cards[i].type == 'm' || cards[i].type == 'c') {
                        return true; }
                    else{
                        //GameLog.Time(mName + " won't work on " + cards[i].name);
                        break; }
                case TargetType.Structure:
                    if (cards[i].type == 's' || cards[i].type == 'n') return true;
                    else break;
                case TargetType.Instant:
                    if (cards[i].type == 'i') return true;
                    else break;
                case TargetType.Minion:
                    if (cards[i].type == 'm' || cards[i].type == 'n') return true;
                    else break;
                case TargetType.MinionStructure:
                    if (cards[i].type == 'n') return true;
                    else break;
                case TargetType.MinionUnit:
                    if (cards[i].type == 'm') return true;
                    else break;
            }
        }
        return false;
    }

    /// <summary>
    /// checks to see if the ability has passed the given condition required to execute
    /// </summary>
    /// <param name="p"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    bool checkCondition(Player p)
    {
        Card TempTriggerCard = mTriggerCard;
        Card TempHolder = mHolder;
        //if popup is active, change the TriggerCard and Holder
        if(PopupManager.mSingleton.mDamagePopup.Card1.name != "Default")
        {
            if(mHolder.is_P1 == PopupManager.mSingleton.mDamagePopup.Card1.is_P1)            
                TempHolder = PopupManager.mSingleton.mDamagePopup.Card1;            
            else if (mTriggerCard.name != "Default" && mTriggerCard.is_P1 == PopupManager.mSingleton.mDamagePopup.Card1.is_P1)
                TempTriggerCard = PopupManager.mSingleton.mDamagePopup.Card1;
            
        }
        if (PopupManager.mSingleton.mDamagePopup.Card2.name != "Default")
        {
            if (mHolder.is_P1 == PopupManager.mSingleton.mDamagePopup.Card2.is_P1)
                TempHolder = PopupManager.mSingleton.mDamagePopup.Card2;
            else if (mTriggerCard.name != "Default" && mTriggerCard.is_P1 == PopupManager.mSingleton.mDamagePopup.Card2.is_P1)
                TempTriggerCard = PopupManager.mSingleton.mDamagePopup.Card2;
        }
        //now get on with the condition checking

            //switch mCondition
        switch (mCondition)
        {
            default:
                return true;
            case "HasEnoughAP":
                if (p.getActionPoints() >= TempHolder.curCost)
                    return true;
                else { fail = "Not enough AP";  return false; } 
            case "UnderHalfHealth":
                if (TempHolder.healthLeft < TempHolder.curHealth * 0.5) //if health remaining is less than half of max health
                    return true;
                else { fail = "Not under half health"; return false; }
            case "HasNotMoved":
                if (TempHolder.movesLeft == TempHolder.curMovement)
                    return true;
                else { fail = "Unit has moved"; return false; }
            case "HasMoved":
                if (TempHolder.movesLeft < TempHolder.curMovement)
                    return true;
                else { fail = "Unit has not moved";  return false; }
            case "HandNotEmpty":
                if (p.deck.Hand.Count > 0)
                    return true;
                else { fail = "Hand is empty";  return false; }
            case "HasEnoughCards":
                if (p.deck.Hand.Count >= mConditionDefine)
                    return true;
                else { fail = "Not enough cards in hand"; return false; }
            case "TurnPlayerHasEnoughUnits":
                if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units.Count >= mConditionDefine)
                    return true;
                else { fail = "Not enough units"; return false; }
            case "OtherPlayerHasEnoughUnits":
                if (GameManager.mSingleton.otherPlayer.unit_mgr.all_units.Count >= mConditionDefine)
                    return true;
                else { fail = "Not enough enemy units"; return false; }
            case "UnitDifference":
                if (GameManager.mSingleton.otherPlayer.unit_mgr.all_units.Count - GameManager.mSingleton.turnPlayer.unit_mgr.all_units.Count >= mConditionDefine)
                    return true;
                else { fail = "Not enough enemy units"; return false; }
            case "UnitDifferenceNotFirstTurn":
                if (GameManager.mSingleton.otherPlayer.unit_mgr.all_units.Count - GameManager.mSingleton.turnPlayer.unit_mgr.all_units.Count >= mConditionDefine
                    && GameManager.mSingleton.curTurn > 1)
                    return true;
                else { fail = ""; return false; }
            case "OtherPlayerHasEnoughStructures":
                if (GameManager.mSingleton.otherPlayer.unit_mgr.all_structures.Count >= mConditionDefine)
                    return true;
                else { fail = "Not enough enemy structures"; return false; }
            case "BothPlayersHavePrisoners":
                bool turn_prisoner = false, other_prisoner = false;
                foreach (Unit u in GameManager.mSingleton.turnPlayer.unit_mgr.all_units)
                {
                    if ( (GameManager.mSingleton.turnPlayer.isP1() && MapManager.isP2Zone(u.curHex))
                        || (!GameManager.mSingleton.turnPlayer.isP1() && MapManager.isP1Zone(u.curHex)) )
                        turn_prisoner = true;
                }
                foreach (Unit u in GameManager.mSingleton.otherPlayer.unit_mgr.all_units)
                {
                    if ((GameManager.mSingleton.otherPlayer.isP1() && MapManager.isP2Zone(u.curHex))
                        || (!GameManager.mSingleton.otherPlayer.isP1() && MapManager.isP1Zone(u.curHex)))
                        other_prisoner = true;
                }
                //return true if both players have prisoners
                if (turn_prisoner && other_prisoner)
                    return true;
                else { fail = ""; return false; }
            case "MinTurn":
                if (GameManager.mSingleton.curTurn >= mConditionDefine)
                    return true;
                else { fail = "Not reached proper turn"; return false; }
            case "CaptureTurn":
                if (GameData.MinCaptureTurn())
                    return true;
                else { fail = "Not yet capture turn"; return false; }
            case "AdjacentAlliedStructure":
                for(int i = 0; i < 7; i++)
                {
                    int adj_hex = TempHolder.mCurHexes[0] + GameData.hexesToCheck[i];
                    if (adj_hex < 0 || adj_hex >= GameData.MAP_SIZE) continue;
                    if (!MapManager.inPixelRange(1, adj_hex, TempHolder.mCurHexes[0])) continue;
                    if (TempHolder.is_P1 && MapManager.MAP1[adj_hex].P1_structureOccupied) return true;
                    else if (!TempHolder.is_P1 && MapManager.MAP1[adj_hex].P2_structureOccupied) return true;
                }
                { fail = "No adjacent allied structures"; return false; }
            case "WillDieFromAttack":
                int damage = (int)GameManager.mSingleton.CalculateDamage(TempTriggerCard.curAttack, TempHolder.curDefence, TempHolder.defenceLost, TempHolder.damageReduction);
                //int damage = PopupManager.mSingleton.mDamagePopup.damage;
                if (damage >= TempHolder.healthLeft && TempHolder.healthLeft > 1) return true;
                else { fail = ""; return false; }
            case "StructureWillDieFromAttack":
                int damage2 = (int)GameManager.mSingleton.CalculateDamage(TempTriggerCard.curAttack, TempHolder.curDefence, TempHolder.defenceLost, TempHolder.damageReduction);
                if (damage2 >= TempHolder.healthLeft) return true;
                else { fail = ""; return false; }
            case "EnemyCommanderNotSpawned":
                if (TempHolder.is_P1 == GameManager.mSingleton.turnPlayer.isP1()) //check p1
                {
                    if (GameManager.mSingleton.otherPlayer.isCommanderFielded || GameManager.mSingleton.otherPlayer.deck.Commander.name == "None") { return false; }
                    else return true;
                }
                else if (TempHolder.is_P1 == GameManager.mSingleton.otherPlayer.isP1())
                {
                    if (GameManager.mSingleton.turnPlayer.isCommanderFielded || GameManager.mSingleton.turnPlayer.deck.Commander.name == "None") { return false; }
                    else return true;
                }
                else { fail = "Enemy Commander has spawned"; return false; }
            case "NeverPlay":
                { fail = ""; return false; }
            case "CanCounterButNotSurvive":
                //IF they can make a normal counterattack,
                if (!TempTriggerCard.counterFirst
                    && GameManager.mSingleton.CanTargetAttack(TempTriggerCard, TempHolder, false))
                {
                    int your_damage = (int)GameManager.mSingleton.CalculateDamage(TempHolder.curAttack, TempTriggerCard.curDefence,
                     TempTriggerCard.defenceLost, TempTriggerCard.damageReduction);
                    //check if you have killed them
                    if (your_damage >= TempTriggerCard.healthLeft)
                    {
                        //if so, run this ability
                        return true;
                    }
                }
                { fail = ""; return false; }
            case "WoodenCoverHasRoom":
                if(TempHolder.is_P1)
                {
                    //check all three hexes facing p2
                    if (MapManager.isEmpty(TempHolder.mCurHexes[0] + (GameData.MAP_COLUMNS * 2 - 1), TempHolder.is_P1, false)
                        && MapManager.isEmpty(TempHolder.mCurHexes[0] + (GameData.MAP_COLUMNS), TempHolder.is_P1, false)
                        && MapManager.isEmpty(TempHolder.mCurHexes[0] + (GameData.MAP_COLUMNS - 1), TempHolder.is_P1, false))
                        return true;
                }
                else
                {
                    //check all three hexes facing p1
                    if (MapManager.isEmpty(TempHolder.mCurHexes[0] + (-GameData.MAP_COLUMNS * 2 + 1), TempHolder.is_P1, false)
                        && MapManager.isEmpty(TempHolder.mCurHexes[0] + (-GameData.MAP_COLUMNS), TempHolder.is_P1, false)
                        && MapManager.isEmpty(TempHolder.mCurHexes[0] + (-GameData.MAP_COLUMNS + 1), TempHolder.is_P1, false))
                        return true;
                }
                { fail = "Not enough room"; return false; }
            case "CommanderIsTrigger":
                if (TempHolder.is_P1)
                {
                    if (!GameManager.mSingleton.PlayerOne.isCommanderFielded) { return false; }
                    for (int i = 0; i < GameManager.mSingleton.PlayerOne.unit_mgr.all_units.Count; i++)
                    {
                        if(GameManager.mSingleton.PlayerOne.unit_mgr.all_units[i].myCard.type == 'c') { 
                        setTriggerCard(GameManager.mSingleton.PlayerOne.unit_mgr.all_units[i].myCard); return true; }
                    }
                }
                else if (!TempHolder.is_P1)
                {
                    if(!GameManager.mSingleton.PlayerTwo.isCommanderFielded) { return false; }
                   for (int i = 0; i < GameManager.mSingleton.PlayerTwo.unit_mgr.all_units.Count; i++)
                    {
                        if(GameManager.mSingleton.PlayerTwo.unit_mgr.all_units[i].myCard.type == 'c') { 
                        setTriggerCard(GameManager.mSingleton.PlayerTwo.unit_mgr.all_units[i].myCard); return true; }
                    }
                }
                return true;
            case "BlockNonCommanders":
                if (TempTriggerCard.type == 'c') return false;
                else return true; 
            case "CommanderNotSpawned":
                if (TempHolder.is_P1 == GameManager.mSingleton.turnPlayer.isP1()) //check p1
                {
                    if (GameManager.mSingleton.turnPlayer.isCommanderFielded) { return false; }
                    else return true;
                }
                else if (TempHolder.is_P1 == GameManager.mSingleton.otherPlayer.isP1())
                {
                    if (GameManager.mSingleton.otherPlayer.isCommanderFielded) { return false; }
                    else return true;
                }
                else { fail = "Commander was spawned"; return false; }
            case "TotalDevastation":
                if (p.deck.Hand.Count >= mConditionDefine)
                { if (TempHolder.is_P1 == GameManager.mSingleton.turnPlayer.isP1()) //check p1
                    {  if (GameManager.mSingleton.turnPlayer.isCommanderFielded) { return false; }
                        else return true; }
                    else if (TempHolder.is_P1 == GameManager.mSingleton.otherPlayer.isP1())
                    {  if (GameManager.mSingleton.otherPlayer.isCommanderFielded) { return false; }
                        else return true; } { return false; } }
                else { fail = ""; return false; }
            case "HasPlayedRune":
                if (p.isRunePlayed) return true;
                else { fail = "Has not played Rune"; return false; }
            case "MaxTriggerRange":
                int attack_range = MapManager.findRange(TempHolder.mCurHexes[0], TempTriggerCard.mCurHexes[0]);
                if (attack_range > mConditionDefine) { fail = "Out of range"; return false; }
                else return true;
            case "HasASpell":
                for(int i = 0; i < GameManager.mSingleton.turnPlayer.deck.Hand.Count; i++)
                {
                    if (GameManager.mSingleton.turnPlayer.deck.Hand[i].type == 'i') return true;
                }
                { fail = "No spells in hand"; return false; }
            case "FireSpellCondition":
                bool found_spell = false;
                for (int i = 0; i < GameManager.mSingleton.turnPlayer.deck.Hand.Count; i++)
                {
                    if (GameManager.mSingleton.turnPlayer.deck.Hand[i].type == 'i') { found_spell = true; break; }
                }
                if(!found_spell)  { fail = "No spells in hand"; return false; } 
                for(int i = 0; i < GameManager.mSingleton.turnPlayer.unit_mgr.all_structures.Count; i++)
                {
                    for(int j = 0; j < GameManager.mSingleton.turnPlayer.unit_mgr.all_structures[i].myCard.mCurHexes.Count; j++)
                    { 
                        if ((MapManager.inRange(1, 10, mHolder.mCurHexes[0], GameManager.mSingleton.turnPlayer.unit_mgr.all_structures[i].myCard.mCurHexes[j])
                            || MapManager.inRange(1, 10, mHolder.mCurHexes[1], GameManager.mSingleton.turnPlayer.unit_mgr.all_structures[i].myCard.mCurHexes[j]))
                            && mHolder.code != GameManager.mSingleton.turnPlayer.unit_mgr.all_structures[i].myCard.code)
                            return true;
                    }
                }
                for (int i = 0; i < GameManager.mSingleton.otherPlayer.unit_mgr.all_structures.Count; i++)
                {
                    for (int j = 0; j < GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[i].myCard.mCurHexes.Count; j++)
                    {
                        GameLog.Write("Is enemy " + GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[i].name + " in range of " +
                            mHolder.mCurHexes[0].ToString() + "?");
                        if (MapManager.inRange(1, 10, mHolder.mCurHexes[0], GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[i].myCard.mCurHexes[j])
                            || MapManager.inRange(1, 10, mHolder.mCurHexes[1], GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[i].myCard.mCurHexes[j]))
                            return true;
                    }
                }
                { fail = "No targets in range"; GameLog.Write("FIRESPELL failed condition"); return false; }
            case "TriggerNotExclusive":
                if (TempTriggerCard.exclusive) { fail = ""; return false; }
                else return true;
            case "TriggerNotMinion":
                if (TempTriggerCard.type == 'm' || TempTriggerCard.type == 'n') { fail = "Card is a minion"; return false; }
                else return true;
            case "NotAtMaxTrees":
                int numTrees = 0;
                for(int i = 0; i < GameManager.mSingleton.turnPlayer.unit_mgr.all_structures.Count; i++)
                {
                    if (GameManager.mSingleton.turnPlayer.unit_mgr.all_structures[i].name == "Sentinel Tree" || GameManager.mSingleton.turnPlayer.unit_mgr.all_structures[i].name == "Sentinel Grove")
                        numTrees++;
                }
                if (numTrees < mConditionDefine) return true;
                else { fail = "Trees at limit"; return false; }
            case "HasUnmovedUnit":
                for(int i = 0; i < GameManager.mSingleton.turnPlayer.unit_mgr.all_units.Count; i++)
                {
                    if (!GameManager.mSingleton.turnPlayer.unit_mgr.all_units[i].turnOver 
                        && GameManager.mSingleton.turnPlayer.unit_mgr.all_units[i].myCard.movesLeft == GameManager.mSingleton.turnPlayer.unit_mgr.all_units[i].myCard.curMovement)
                        return true;
                }
                { fail = "No unmoved units"; return false; }
            case "HasEnoughUP":
                if (p.getCurrentUP() >= mConditionDefine)
                    return true;
                else { fail = "Not enough UP"; return false; }
            case "HasStormNotWolf":
                foreach(Unit u in GameManager.mSingleton.turnPlayer.unit_mgr.all_units)
                {
                    if (u.myCard.HasFaction(Faction.Storm) && u.myCard.identifier != "WolfRaiju") return true;
                }
                { fail = "No Storm units"; return false; }
            case "DontPayCostFalse":
                if (!GameManager.mSingleton.Dont_Pay_Cost) return true;
                else return false;
            case "TrueAimNumberAtZero":
                foreach(AbilityBlock ab in mHolder.mAbilities)
                {
                    if (ab.mName == "TrueAimNumber" && ab.mTimesLeft > 0) return false;
                }
                return true;
            case "TrueAimNumberAboveZero":
                foreach (AbilityBlock ab in mHolder.mAbilities)
                {
                    if (ab.mName == "TrueAimNumber" && ab.mTimesLeft > 0) return true;
                }
                return false;
            case "HasEnoughCharge":
                if (mHolder.countAbility("StormChargeBoost") >= mConditionDefine) return true;
                else { fail = "Magical Charge is not fully charged"; return false; }
            case "ChariotEnable":
                if (mHolder.is_P1)
                {
                    int hex_to_check = mHolder.mCurHexes[0] + GameData.hexesToCheck[1];
                    if (MapManager.isValid(hex_to_check, true, false) || MapManager.isP1Unit(hex_to_check)) return true;
                    else return false;
                }
                else
                {
                    int hex_to_check = mHolder.mCurHexes[0] + GameData.hexesToCheck[6];
                    if (MapManager.isValid(hex_to_check, true, false) || MapManager.isP2Unit(hex_to_check)) return true;
                    else return false;
                }
            case "ChariotDisable":
                if (mHolder.is_P1)
                {
                    int hex_to_check = mHolder.mCurHexes[0] + GameData.hexesToCheck[1];
                    if (!MapManager.isValid(hex_to_check, true, false) || MapManager.isP2Unit(hex_to_check) ) return true;
                    else return false;
                }
                else
                {
                    int hex_to_check = mHolder.mCurHexes[0] + GameData.hexesToCheck[6];
                    if (!MapManager.isValid(hex_to_check, true, false) || MapManager.isP1Unit(hex_to_check) ) return true;
                    else return false;
                }
            case "ChariotEnable2":
                if (mHolder.is_P1)
                {
                    int hex_to_check = mHolder.mCurHexes[0] + GameData.hexesToCheck[1];
                    if (MapManager.isP1Unit(hex_to_check)) return true;
                    else return false;
                }
                else
                {
                    int hex_to_check = mHolder.mCurHexes[0] + GameData.hexesToCheck[6];
                    if (MapManager.isP2Unit(hex_to_check)) return true;
                    else return false;
                }
            case "ChariotDisable2":
                if (mHolder.is_P1)
                {
                    int hex_to_check = mHolder.mCurHexes[0] + GameData.hexesToCheck[1];
                    if (!MapManager.isP1Unit(hex_to_check)) return true;
                    else return false;
                }
                else
                {
                    int hex_to_check = mHolder.mCurHexes[0] + GameData.hexesToCheck[6];
                    if (!MapManager.isP2Unit(hex_to_check)) return true;
                    else return false;
                }
            case "EnchantCondition":
                hexRange closest_spire = MapManager.GetClosestSpire(mHolder.mCurHexes[0]);
                if (p.deck.Hand.Count == 0) //fail is hand has no cards
                { fail = "Hand is empty"; return false; }
                else if(GameManager.mSingleton.curTurn < GameData.MIN_CAPTURE_TURN) { fail = "Cannot capture spires yet"; return false; }
                else if (closest_spire.range > 0 && closest_spire.range <= mConditionDefine) return true;
                else { fail = "No spire in range"; return false; } //fail if no spire in range
            case "ControlsBothSpires":
                if (p.ControlsLeftSpawn() && p.ControlsRightSpawn()) return true;
                else return false;
            case "NotLastOne":
                for(int i = 0; i < p.unit_mgr.all_structures.Count; i++)
                {
                    if (p.unit_mgr.all_structures[i].name == mHolder.name && p.unit_mgr.all_structures[i].code != mHolder.code) return true;
                }
                for (int i = 0; i < p.unit_mgr.all_units.Count; i++)
                {
                    if (p.unit_mgr.all_units[i].name == mHolder.name && p.unit_mgr.all_units[i].code != mHolder.code) return true;
                }
                return false;
            case "ArionLives":
                for (int i = 0; i < p.unit_mgr.all_units.Count; i++)
                {
                    if (p.unit_mgr.all_units[i].name == "Arion") return true;
                }
                return false;
            case "JimmuLives":
                for (int i = 0; i < p.unit_mgr.all_units.Count; i++)
                {
                    if (p.unit_mgr.all_units[i].name == "Emperor Jimmu") return true;
                }
                return false;
            case "PlayerOneTurn":
                if (GameManager.mSingleton.turnPlayer.isP1()) return true;
                else return false;
                //new case here
        }
    }

    /// <summary>
    /// Executes all AbilityChunks, adding them to the appropriate lists.
    /// </summary>
    public void Execute(ref Player player, int curHex) { Execute(ref player, curHex, false); }
    public void Execute(ref Player player, int curHex, bool skipMapTargetChunk)
    {
        if (checkGlobalTimes()
            && mDelayed == 0 //if this ability is not delayed
            && checkCooldown() //check cooldown
            && checkTimes() //check times
            && checkTriggerCardType()
            && checkZone(player.isP1(), curHex) //check proper zone
            && checkCost(player.getActionPoints()) //check cost     
            && checkXPCost(player.curXP)
            && checkTargets()
            && checkCondition(player)
            )
        {
            if (mName != "Update" && mTrigger != TriggerTime.Continuous)
            {
             //   GameLog.Record(mHolder.name + " is executing " + mDisplayName);
             //  GameLog.Write(mHolder.name + " is executing " + mDisplayName);
            }

            //negation is checked now in execute, to allow for some chunks to ignore negation
            if (!mHolder.negated)
            {
                adjustInfo(ref player);
                if (!delayedAbility) //don't double up on delayed messages
                    PopupManager.mSingleton.setupMessage(mMessage, MyMessageType.Ability);
                //add here
            }

            foreach (AbilityChunk pAbility in mAbilityList)
            {
                //skip if card is negated OR we ignore negation
                if (mHolder.negated && !pAbility.mIgnoreNegation) continue;

                //skip map target chunk if we have flagged bool
                if (skipMapTargetChunk && pAbility.mTargetLocation == TargetLocation.Map) continue;

                //otherwise run normally

                //set the times for each chunk when we execute the ability block
                pAbility.mTimesLeft = pAbility.mTimesAvailable;

                //make sure the effect can run
                pAbility.VisualEffectRun = false;

                //if (mTriggerCard.name != "Default")
                {
                    pAbility.mTriggerCard = mTriggerCard;
                }

                // do stuff here, adding to correct lists
                if (pAbility.mTrigger == TriggerTime.End)
                {
                    AbilityHandler.mEndOfTurnTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Start)
                {
                    AbilityHandler.mStartOfTurnTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Transition)
                {
                    AbilityHandler.mTransitionTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Continuous)
                {
                 //   Debug.Log("Adding chunk to continuous triggers");
                    AbilityHandler.mContinuousAbilities.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.MoveEnd)
                {
                    AbilityHandler.mMoveEndTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Death)
                {
                    AbilityHandler.mDeathTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Reset)
                {
                    AbilityHandler.mResetTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.EnemySpawn)
                    AbilityHandler.mEnemySpawnTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.MySpawn)
                    AbilityHandler.mMySpawnTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.EnemyDeath)
                    AbilityHandler.mEnemyDeathTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.MyDeath)
                    AbilityHandler.mMyDeathTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.MySpell)
                    AbilityHandler.mMySpellTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.EnemySpell)
                    AbilityHandler.mEnemySpellTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.AllSpell)
                    AbilityHandler.mAllSpellTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.AllSpawn)
                    AbilityHandler.mAllSpawnTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.AllDeath)
                    AbilityHandler.mAllDeathTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.Counterattacked)
                    AbilityHandler.mCounterattackedTriggers.Add(pAbility);
                else
                {
                    //priority ability
                    if (mPriorityAbility)
                    {
                        AbilityHandler.mResolvingAbilities.Insert(0, pAbility); //add this ability to the front
                        AbilityHandler.ResolveAbilities(false); //and immediately try to resolve it
                    }
                    //normal ability
                    else
                        AbilityHandler.mResolvingAbilities.Add(pAbility);
                }

                //if this ability chunk needs the list of runes, pull it up
                if (pAbility.mTargetLocation == TargetLocation.AllRunes)
                {
                    if (pAbility.mTargetPlayer == TargetPlayer.AllAllies) GameManager.mSingleton.turnPlayer.deck.LoadAllRunes(GameManager.mSingleton.turnPlayer.isP1());
                    else if (pAbility.mTargetPlayer == TargetPlayer.AllEnemies) GameManager.mSingleton.turnPlayer.deck.LoadAllRunes(GameManager.mSingleton.otherPlayer.isP1());
                }

            }
        } //end if check
        else if (mHolder.negated)
        {
            //card is negated
        }
        else if (mDelayed > 0 && mDelayRemaining == 0) //set the delay timer
        {
            PopupManager.mSingleton.setupMessage(mMessage, MyMessageType.Ability);
            if (mDescription.Length > 0) GameLog.Display(mDisplayName + " will activate later", false, false, false);
            player.exceedPoints(-1 * mCost); //pay the cost now
            mDelayRemaining = mDelayed;
            AbilityHandler.AbilitiesUsed.Add(mName); //still want delayed abilities to count towards the global times

            //handle delayed spells
        //    if (mHolder.type == 'i')AbilityHandler.mDelayedAbilities.Add(this);
        }
        //checks to see WHY the ability failed so we can inform the user
        //else if(mTrigger == TriggerTime.Instant)
        //{ 
            else if (!checkCooldown())
            {
                GameLog.Display(mDisplayName + " is still on cooldown", false, true, true);
            }
            else if (!checkTimes() || !checkGlobalTimes())
            {
                GameLog.Display(mDisplayName + " has no uses left", false, true, true);
            }
            else if (!checkTriggerCardType())
            {
                //This doesn't need a display since these aren't activated by the user
            }
            else if (!checkZone(player.isP1(), curHex))
            {
                GameLog.Display("The ability holder is in the wrong zone", false, true, true);
            }
            else if (!checkCost(player.getActionPoints()))
            {
                GameLog.Display("You do not have enough Action Points", false, true, true);
            }
            else if (!checkTargets())
            {
                GameLog.Display(mDisplayName + " does not have enough valid targets", false, true, true);
            }
            else if (!checkXPCost(player.curXP))
            {
                GameLog.Display("You do not have enough XP", false, true, true);
            }
        //}



    } //end execute function

    /// <summary>
    /// Used in damage display
    /// </summary>
    /// <param name="player"></param>
    /// <param name="curHex"></param>
    public void ExecuteDamageDisplay(ref Player player, int curHex)
    {
        if (checkGlobalTimes()
            && mDelayed == 0 //if this ability is not delayed
            && checkCooldown() //check cooldown
            && checkTimes() //check times
            && checkTriggerCardType()
            && checkZone(player.isP1(), curHex) //check proper zone
            && checkCost(player.getActionPoints()) //check cost     
            && checkXPCost(player.curXP)
            && checkTargets()
            && checkCondition(player)
            )
        {
            foreach (AbilityChunk pAbility in mAbilityList)
            {
                //skip chunks if we are negated
                if (mHolder.negated && !pAbility.mIgnoreNegation) continue;

                //skip chunks that do not target self or the trigger card
                if (pAbility.mTargetType != TargetType.Self && pAbility.mTargetType != TargetType.TriggerCard) continue;

                //skip any ability that does not happen at battle time
                if (pAbility.mTrigger != TriggerTime.Instant) continue;

                //also skip chunks unless they are relevant to the damage calculation
                if (pAbility.mEffect != EffectType.AttackMod && pAbility.mEffect != EffectType.AttackMult && pAbility.mEffect != EffectType.DefenceMod &&
                    pAbility.mEffect != EffectType.DefenceMult && pAbility.mEffect != EffectType.BuffMult && pAbility.mEffect != EffectType.DamageReduction
                    && pAbility.mEffect != EffectType.HealthMod && pAbility.mEffect != EffectType.CompareStats)
                    continue;

                //set up damage checks if the effect is healthmod
                //we set health to 1000 so the card doesn't die from the ability
                int curHP = 0, curMaxHP = 0;
                if(pAbility.mEffect == EffectType.HealthMod)
                {
                    if(pAbility.mTargetType == TargetType.TriggerCard)
                    {
                        curHP = mTriggerCard.healthLeft;
                        curMaxHP = mTriggerCard.curHealth;
                        mTriggerCard.healthLeft = 1000;
                        mTriggerCard.curHealth = 2000;
                    }
                    else //only other option is self
                    {
                        curHP = mHolder.healthLeft;
                        curMaxHP = mHolder.curHealth;
                        mHolder.healthLeft = 1000;
                        mHolder.curHealth = 2000;
                    }
                }

                //don't alter base stats
                if (pAbility.mAffectsBaseStats) pAbility.mAffectsBaseStats = false;

                //run ability
                AbilityHandler.mResolvingAbilities.Insert(0, pAbility); //add this ability to the front
                AbilityHandler.ResolveAbilities(false); //and immediately try to resolve it

                //if effect is healthmod, check how much damage was done
                if(pAbility.mEffect == EffectType.HealthMod)
                {
                    if (pAbility.mTargetType == TargetType.TriggerCard)
                    {
                        int triggerHealthLoss = 1000 - mTriggerCard.healthLeft;
                        mTriggerCard.healthLeft = curHP;
                        mTriggerCard.curHealth = curMaxHP;
                        if (pAbility.mTertiaryValue > 0 && triggerHealthLoss >= mTriggerCard.healthLeft) triggerHealthLoss = mTriggerCard.healthLeft - 1;
                        if (mTrigger == TriggerTime.Attacking || mTrigger == TriggerTime.Counterattacked || (mTrigger == TriggerTime.Death && GameManager.mSingleton.turnPlayer.isP1() == player.isP1()))
                            PopupManager.mSingleton.mDamagePopup.effect += triggerHealthLoss;
                        else PopupManager.mSingleton.mDamagePopup.counterEffect += triggerHealthLoss;
                    }
                    else //only other option is self
                    {
                        int selfHealthLoss = 1000 - mHolder.healthLeft;
                        mHolder.healthLeft = curHP;
                        mHolder.curHealth = curMaxHP;
                        if (pAbility.mTertiaryValue > 0 && selfHealthLoss >= mHolder.healthLeft) selfHealthLoss = mHolder.healthLeft - 1;
                        if (mTrigger == TriggerTime.Attacked || mTrigger == TriggerTime.Counterattacking || (mTrigger == TriggerTime.Death))
                            PopupManager.mSingleton.mDamagePopup.effect += selfHealthLoss;
                        else PopupManager.mSingleton.mDamagePopup.counterEffect += selfHealthLoss;
                    }
                }
                       
            }
        } //end if check
    } //end special execute function


    public bool checkTargets()
    {


        return true;
    }

    public bool checkTimes()
    {
        if (mTimesLeft == -1)
            return true;
        else if (mTimesLeft > 0)
            return true;
        else
        {
            return false;
        }
    }

    public bool checkTriggerCardType()
    {
        if (mTriggerCardType == TargetType.All)
            return true;
        else if (mTriggerCard.name != "Default")
        {
            switch (mTriggerCardType)
            {
                case TargetType.Self:
                    if (mTriggerCard.code == mHolder.code) return true;
                    else return false;
                case TargetType.Unit:
                    if (mTriggerCard.getType() == 'u' || mTriggerCard.getType() == 'm' || mTriggerCard.getType() == 'c')
                        return true;
                    else return false;
                case TargetType.Structure:
                    if (mTriggerCard.getType() == 's' || mTriggerCard.getType() == 'n')
                        return true;
                    else return false;
                case TargetType.Minion:
                    if (mTriggerCard.getType() == 'n' || mTriggerCard.getType() == 'm')
                        return true;
                    else return false;
                case TargetType.Commander:
                    if (mTriggerCard.getType() == 'c')
                        return true;
                    else return false;
            }
            return false;
        }
        else
            return true;
    }

    public bool checkCost(int num_points)
    {
        if (num_points >= mCost)
            return true;
        else
        {
            return false;
        }
    }

    public bool checkXPCost(int xp)
    {
        if (xp >= mXPCost)
            return true;
        else
        {
            return false;
        }
    }

    public bool checkCooldown()
    {
        if (mCooldownRemaining == 0)
            return true;
        else
            return false;
    }

    public bool checkZone(bool P1, int curHex)
    {
        if (curHex < 0)
        {
            return true;
        }
        else if (mZone == AffectedZone.Both)
        {
            return true;
        }
        else if (mZone == AffectedZone.Enemy)
        {
            if ((!P1 && MapManager.isP1Zone(curHex)) || (P1 && MapManager.isP2Zone(curHex)))
                return true;
            else
                return false;
        }
        else if (mZone == AffectedZone.Player)
        {
            if ((P1 && MapManager.isP1Zone(curHex)) || (!P1 && MapManager.isP2Zone(curHex)))
                return true;
            else
                return false;
        }
        else if (mZone == AffectedZone.Neutral)
        {
            if (!MapManager.isP1Zone(curHex) && !MapManager.isP2Zone(curHex))
                return true;
            else
                return false;
        }
        else if (mZone == AffectedZone.NotPlayer)
        {
            if ((P1 && !MapManager.isP1Zone(curHex)) || (!P1 && !MapManager.isP2Zone(curHex)))
                return true;            
            else 
                return false;            
        }
        else if (mZone == AffectedZone.NotEnemy)
        {
            if ((!P1 && !MapManager.isP1Zone(curHex)) || (P1 && !MapManager.isP2Zone(curHex)))
                return true;
            else
                return false;
        }
        return true;
    }

    public bool checkGlobalTimes()
    {
        if (mGlobalTimes < 0)
            return true;
        else
        {
            int count = 0;
            for(int i = 0; i < AbilityHandler.AbilitiesUsed.Count; i++)
            {
                if (mName == AbilityHandler.AbilitiesUsed[i])
                    count++;
            }
            if (count >= mGlobalTimes)
                return false;
            else
                return true;
        }
    }

    public void adjustInfo(ref Player curPlayer)
    {
        if (mTimesLeft != -1 && mTrigger != TriggerTime.Continuous)
        {
            mTimesLeft--;
        }
        curPlayer.exceedPoints(-1 * mCost);
        mCooldownRemaining = mCooldown;
        curPlayer.curXP -= mXPCost;

        //add the ability to abilitiesUsed
        if (!delayedAbility) //delayed abilities are checked when they are activated
        {
            AbilityHandler.AbilitiesUsed.Add(mName);
        }
    }

    public void setCode(int num)
    {
        mCode = num;
        for(int i = 0; i < mAbilityList.Count; i++)
        {
            mAbilityList[i].mCode = num;
        }
    }
    public void setTriggerCard(Card pTriggerCard)
    {
        mTriggerCard = pTriggerCard;
        for (int i = 0; i < mAbilityList.Count; i++)
        {
            mAbilityList[i].mTriggerCard = pTriggerCard;
        }
    }

    public ushort GetID() {
        return mID; 
    }

    public void ExecuteNoChecks(ref Player player, int curHex)
    {
        {
            //if (mName != "Update" && mTrigger != TriggerTime.Continuous)
            {
                //GameLog.Record(mHolder.name + " is executing " + mName);
                //GameLog.Write(mHolder.name + " is executing " + mName);
            }
            foreach (AbilityChunk pAbility in mAbilityList)
            {
                //set the times for each chunk when we execute the ability block
                pAbility.mTimesLeft = pAbility.mTimesAvailable;

                //if (mTriggerCard.name != "Default")
                {
                    pAbility.mTriggerCard = mTriggerCard;
                }

                // do stuff here, adding to correct lists
                if (pAbility.mTrigger == TriggerTime.End)
                {
                    AbilityHandler.mEndOfTurnTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Start)
                {
                    AbilityHandler.mStartOfTurnTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Transition)
                {
                    AbilityHandler.mTransitionTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Continuous)
                {
                    AbilityHandler.mContinuousAbilities.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.MoveEnd)
                {
                    AbilityHandler.mMoveEndTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Death)
                {
                    AbilityHandler.mDeathTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.Reset)
                {
                    AbilityHandler.mResetTriggers.Add(pAbility);
                }
                else if (pAbility.mTrigger == TriggerTime.EnemySpawn)
                    AbilityHandler.mEnemySpawnTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.MySpawn)
                    AbilityHandler.mMySpawnTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.EnemyDeath)
                    AbilityHandler.mEnemyDeathTriggers.Add(pAbility);
                else if (pAbility.mTrigger == TriggerTime.MyDeath)
                    AbilityHandler.mMyDeathTriggers.Add(pAbility);
                else
                {
                    //priority ability
                    if (mPriorityAbility)
                    {
                        AbilityHandler.mResolvingAbilities.Insert(0, pAbility); //add this ability to the front
                        AbilityHandler.ResolveAbilities(false); //and immediately try to resolve it
                    }
                    //normal ability
                    else
                        AbilityHandler.mResolvingAbilities.Add(pAbility);
                }
            }
        } //end if check     
    } //end execute function



}