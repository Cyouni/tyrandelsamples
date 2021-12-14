using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum GoalType
{
    AttackBase = 100, CaptureSpire = 80, ProtectBase = 90, DestroyUnit = 85, EarlyCapture = 60,
    AttackUnit = 75, DestroyStructure = 70, AttackStructure = 62, Escape = 78, Ability = 74, None = 0, ProtectCommander = 89
    //CaptureMiddleSpire?
}

/// <summary>
/// Used to calculate what card the AI wants to play
/// </summary>
public enum CardTag
{
    None, Attack, Defend, Damage, Block, Support, Heal, Boost,
    Commander, Unit, Structure, Instant, MinionUnit, MinionStructure,
    PerCard
}

public class Goal
{
    public GoalType mType;
    public float mScore;
    public int mUnit; //index
    public int mOther; //index of enemy unit, structure, etc

    public Card mCard; //for ability targets

    public Goal()
    {
        mType = GoalType.None;
        mScore = 0;
        mUnit = 0;
        mOther = -1;
        mCard = new Card();
    }

    public Goal(Goal g)
    {
        mType = g.mType;
        mScore = g.mScore;
        mUnit = g.mUnit;
        mOther = g.mOther;
        mCard = g.mCard;
    }
}

public static class GoalManager
{
    public static List<Goal> PossibleGoals = new List<Goal>();
    public static List<Goal> AssignedGoals = new List<Goal>();
    public static List<int> MoveTargets = new List<int>();
    public static List<Unit> mNearbyAllyUnits = new List<Unit>();
    public static List<Unit> mNearbyEnemyUnits = new List<Unit>();
    public static List<Structure> mNearbyAllyStructs = new List<Structure>();
    public static List<Structure> mNearbyEnemyStructs = new List<Structure>();

    /// <summary>
    /// clears the lists
    /// </summary>
    public static void Clear()
    {
        AssignedGoals.Clear();
        PossibleGoals.Clear();
        MoveTargets.Clear();
    }

    /// <summary>
    /// Finds allied/enemy objects near a particular hex
    /// </summary>
    /// <param name="pHex"></param>
    /// <param name="pDistance"></param>
    public static void FindNearbyObjects(int pHex, int pDistance)
    {
        // clear lists
        mNearbyAllyUnits.Clear();
        mNearbyEnemyUnits.Clear();
        mNearbyAllyStructs.Clear();
        mNearbyEnemyStructs.Clear();

        // loop through units, add to list
        foreach (Unit pUnit in GameManager.mSingleton.turnPlayer.unit_mgr.all_units)
        {
            // checks if in range
            if (MapManager.findRange(pUnit.getHex(), pHex) <= pDistance)
            {
                mNearbyAllyUnits.Add(pUnit);
            }
        }

        foreach (Unit pUnit in GameManager.mSingleton.otherPlayer.unit_mgr.all_units)
        {
            // checks if in range
            if (MapManager.findRange(pUnit.getHex(), pHex) <= pDistance)
            {
                mNearbyEnemyUnits.Add(pUnit);
            }
        }

        foreach (Structure pStruct in GameManager.mSingleton.turnPlayer.unit_mgr.all_structures)
        {
            // checks if in range
            foreach (int pStructHex in pStruct.getHexes())
            {
                if (MapManager.findRange(pStructHex, pHex) <= pDistance)
                {
                    mNearbyAllyStructs.Add(pStruct);
                    break;
                }
            }
        }

        foreach (Structure pStruct in GameManager.mSingleton.otherPlayer.unit_mgr.all_structures)
        {
            // checks if in range
            foreach (int pStructHex in pStruct.getHexes())
            {
                if (MapManager.findRange(pStructHex, pHex) <= pDistance)
                {
                    mNearbyEnemyStructs.Add(pStruct);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Given a goal, finds AI inclination towards that goal
    /// </summary>
    /// <param name="pGoal"></param>
    /// <returns></returns>
    public static float GetScoreForGoal(GoalType pGoal)
    {
        int pHexDistance = 9; // temp, adjust for enemies
        bool pIsEnemyHeavy = false;
        float pScore = 0f;

        switch (pGoal)
        {
            case GoalType.ProtectBase:
                // populate lists to calculate, uses own base
                if (GameManager.mSingleton.turnPlayer.isP1())
                    FindNearbyObjects(MapManager.P1_Base, pHexDistance);
                else
                    FindNearbyObjects(MapManager.P2_Base, pHexDistance);

                pScore = CalculateUnitOffset(ref pIsEnemyHeavy);
                Debug.Log("Protect Base: " + CalculateUnitOffset(ref pIsEnemyHeavy) + " E:" + pIsEnemyHeavy);

                // slant towards this action if enemy heavy near base
                if (pIsEnemyHeavy) return pScore + 100;
                break;
            case GoalType.EarlyCapture:
            case GoalType.CaptureSpire:
                for (int i = 0; i < MapManager.CAPTURE_HEXES.Count; i++)
                {
                    // populate lists to calculate
                    FindNearbyObjects(MapManager.CAPTURE_HEXES[i].hex, pHexDistance);

                    // calculate score
                    float pTempScore = CalculateUnitOffset(ref pIsEnemyHeavy);
                    Debug.Log("Capture Spire " + i + ": " + pTempScore + " E:" + pIsEnemyHeavy);
                    if (pTempScore > pScore) pScore = pTempScore;
                }
                return pScore;
            case GoalType.AttackBase:
                // populate lists to calculate
                if (GameManager.mSingleton.turnPlayer.isP1())
                    FindNearbyObjects(MapManager.P2_Base, pHexDistance);
                else
                    FindNearbyObjects(MapManager.P1_Base, pHexDistance);

                Debug.Log("Attack Base: " + CalculateUnitOffset(ref pIsEnemyHeavy) + " E:" + pIsEnemyHeavy);
                break;
        }

        return pScore;
    }

    /// <summary>
    /// Uses list of nearby units/structures to figure how relevant it is, assuming a round of attacks.
    /// </summary>
    /// <returns></returns>
    public static float CalculateUnitOffset(ref bool pIsEnemyHeavy)
    {
        if (mNearbyAllyUnits.Count == 0 && mNearbyEnemyUnits.Count == 0)
        {
            pIsEnemyHeavy = false;
            return 100;
        }

        // ======= CALCULATE ALLIES ============

        // calculate average ally def
        float pAvgAllyUnitDef = 0;
        float pTotalAllyHP = 0;
        foreach (Unit pUnit in mNearbyAllyUnits)
        {
            pAvgAllyUnitDef += pUnit.getDefence();
            pTotalAllyHP += pUnit.getHealthLeft();
        }
        if (mNearbyAllyUnits.Count > 0) pAvgAllyUnitDef /= mNearbyAllyUnits.Count;

        // calculates a round of attacks given the enemy atk and avg ally def
        float pEnemyAttackPower = 0;
        foreach (Unit pUnit in mNearbyEnemyUnits) pEnemyAttackPower += pUnit.getAttack() - pAvgAllyUnitDef;
        pTotalAllyHP -= pEnemyAttackPower;

        // ======= CALCULATE ENEMIES ============

        // calculate average enemy def
        float pAvgEnemyUnitDef = 0;
        float pTotalEnemyHP = 0;
        foreach (Unit pUnit in mNearbyEnemyUnits)
        {
            pAvgEnemyUnitDef += pUnit.getDefence();
            pTotalEnemyHP += pUnit.getHealthLeft();
        }
        if (mNearbyEnemyUnits.Count > 0)
            pAvgEnemyUnitDef /= mNearbyEnemyUnits.Count;

        // calculates a round of attacks given the ally atk and avg enemy def
        float pAllyAttackPower = 0;
        foreach (Unit pUnit in mNearbyAllyUnits) pAllyAttackPower += pUnit.getAttack() - pAvgEnemyUnitDef;
        pTotalEnemyHP -= pAllyAttackPower;

        // ======= TABULATE RESULTS =============

        // if enemies would generally win
        if ((pTotalEnemyHP - pAllyAttackPower) > 0 && pTotalAllyHP < 0)
        {
            pIsEnemyHeavy = true;
            return pTotalEnemyHP - pTotalAllyHP;
        }
        else {
            // if allies are stronger
            if (pTotalAllyHP > pTotalEnemyHP)
            {
                return pTotalAllyHP - pTotalEnemyHP;
            }
            else {
                pIsEnemyHeavy = true;
                return pTotalEnemyHP - pTotalAllyHP;
            }
        }
    }

    public static GoalType GetGlobalGoal()
    {
        GoalType pGoal = GoalType.None;
        float pScore = 0;

        foreach (GoalType pTargetGoal in Enum.GetValues(typeof(GoalType)))
        {
            float pNewScore = GetScoreForGoal(pTargetGoal);
            Debug.Log(pTargetGoal.ToString() + ": " + pNewScore);
            if (pNewScore > pScore)
            {
                pScore = pNewScore;
                pGoal = pTargetGoal;
                Debug.Log(pTargetGoal.ToString() + " becomes new Goal");
            }
        }

        return pGoal;
    }

    /// <summary>
    /// Assign an objective to each unit
    /// </summary>
    public static void AssignGoals()
    {
        Goal temp_goal = new Goal();
        //for every unit the AI controls

        GameLog.RecordAI("------------------------\nGoal scores for " + GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].name);
        int pos = GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].getHex();

        temp_goal.mUnit = AIManager.curUnit;

        //-----------------
        //DAMAGE ENEMY BASE

        //get the hex location of the enemy base
        int enemyBaseHex;
        if (GameManager.mSingleton.turnPlayer.isP1())
            enemyBaseHex = MapManager.P2_Base;
        else
            enemyBaseHex = MapManager.P1_Base;

        //calculate the unit's distance from it
        float distance = MapManager.findRange(pos, enemyBaseHex);
        float priority = MissionManager.CheckAIGoal(GoalType.AttackBase); //attacking enemy base has the highest priority
        temp_goal.mType = GoalType.AttackBase;
        float modifier = 0;

        temp_goal.mScore = (priority + modifier) / distance;
        PossibleGoals.Add(new Goal(temp_goal));
        GameLog.RecordAI("Attack Base: " + temp_goal.mScore.ToString() + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());

        //-----------------
        //CAPTURE SPIRES

        //for each spire, calculate the score
        if (MapManager.CAPTURE_HEXES.Count < 1) goto AttackUnits;
        distance = MapManager.findRange(pos, MapManager.CAPTURE_HEXES[0].hex);
        modifier = 0;

        //Don't set this goal if it is neutral and we want to contest
        //I can check if it is neutral, but I don't know right now whether we will capture or contest it
        //Check for enemy adjacencies here I guess

        //set the priority
        if (GameManager.mSingleton.curTurn >= GameData.MIN_CAPTURE_TURN && !GameManager.mSingleton.turnPlayer.ControlsSpire(GameData.SPIRE_SET[0]) && !AIManager.AlreadyContested(0))
        {
            priority = MissionManager.CheckAIGoal(GoalType.CaptureSpire);
            temp_goal.mType = GoalType.CaptureSpire;
        }
        else if (GameManager.mSingleton.curTurn >= GameData.MIN_CAPTURE_TURN)
        {
            priority = MissionManager.CheckAIGoal(GoalType.None);
            temp_goal.mType = GoalType.None;
        }
        else
        {
            priority = MissionManager.CheckAIGoal(GoalType.EarlyCapture);
            temp_goal.mType = GoalType.EarlyCapture;
        }

        temp_goal.mScore = (priority + modifier) / distance;
        temp_goal.mOther = 0;
        PossibleGoals.Add(new Goal(temp_goal));
        GameLog.RecordAI("Capture L Spire: " + temp_goal.mScore.ToString() + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());

        //now spire 2
        if (MapManager.CAPTURE_HEXES.Count < 2) goto AttackUnits;

        //it may have a different priority
        if (GameManager.mSingleton.curTurn >= GameData.MIN_CAPTURE_TURN && !GameManager.mSingleton.turnPlayer.ControlsSpire(GameData.SPIRE_SET[1]) && !AIManager.AlreadyContested(1))
        {
            priority = MissionManager.CheckAIGoal(GoalType.CaptureSpire);
            temp_goal.mType = GoalType.CaptureSpire;
        }
        else if (GameManager.mSingleton.curTurn >= GameData.MIN_CAPTURE_TURN)
        {
            priority = MissionManager.CheckAIGoal(GoalType.None);
            temp_goal.mType = GoalType.None;
        }
        else
        {
            priority = MissionManager.CheckAIGoal(GoalType.EarlyCapture);
            temp_goal.mType = GoalType.EarlyCapture;
        }

        distance = MapManager.findRange(pos, MapManager.CAPTURE_HEXES[1].hex);
        modifier = 0;
        temp_goal.mScore = (priority + modifier) / distance;
        temp_goal.mOther = 1;
        PossibleGoals.Add(new Goal(temp_goal));
        GameLog.RecordAI("Capture M Spire: " + temp_goal.mScore.ToString() + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());

        //now spire 3
        if (MapManager.CAPTURE_HEXES.Count < 3) goto AttackUnits;
        //set the priority
        if (GameManager.mSingleton.curTurn >= GameData.MIN_CAPTURE_TURN && !GameManager.mSingleton.turnPlayer.ControlsSpire(GameData.SPIRE_SET[2]) && !AIManager.AlreadyContested(2))
        {
            priority = MissionManager.CheckAIGoal(GoalType.CaptureSpire);
            temp_goal.mType = GoalType.CaptureSpire;
        }
        else if (GameManager.mSingleton.curTurn >= GameData.MIN_CAPTURE_TURN)
        {
            priority = MissionManager.CheckAIGoal(GoalType.None);
            temp_goal.mType = GoalType.None;
        }
        else
        {
            priority = MissionManager.CheckAIGoal(GoalType.EarlyCapture);
            temp_goal.mType = GoalType.EarlyCapture;
        }

        distance = MapManager.findRange(pos, MapManager.CAPTURE_HEXES[2].hex);
        modifier = 0;
        temp_goal.mScore = (priority + modifier) / distance;
        temp_goal.mOther = 2;
        PossibleGoals.Add(new Goal(temp_goal));
        GameLog.RecordAI("Capture R Spire: " + temp_goal.mScore.ToString() + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());

    //-----------------
    //ATTACK UNITS
    AttackUnits:

        //for each enemy unit, calculate the score
        for (int j = 0; j < GameManager.mSingleton.otherPlayer.unit_mgr.all_units.Count; j++)
        {
            int enemy_hex = GameManager.mSingleton.otherPlayer.unit_mgr.all_units[j].getHex();
            distance = MapManager.findRange(pos, enemy_hex);
            modifier = GetAttackRating(GameManager.mSingleton.otherPlayer.unit_mgr.all_units[j].myCard);

            //reduce the likelihood of attacking a unit that has more defence than your attack
            int damageModifier = GameManager.mSingleton.otherPlayer.unit_mgr.all_units[j].myCard.curDefence - GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.curAttack;
            if (damageModifier > 0)
                modifier -= damageModifier;

            temp_goal.mOther = j;

            //if the enemy unit in our zone, value that target higher
            if ((GameManager.mSingleton.turnPlayer.isP1() && MapManager.isP1Zone(enemy_hex)) || (!GameManager.mSingleton.turnPlayer.isP1() && MapManager.isP2Zone(enemy_hex)))
            {
                priority = MissionManager.CheckAIGoal(GoalType.ProtectBase);
                temp_goal.mType = GoalType.ProtectBase;
            }
            //determine if this attack will destroy the other unit or not
            else if (GameManager.mSingleton.WillTargetDie(GameManager.mSingleton.otherPlayer.unit_mgr.all_units[j], GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit]))
            {
                priority = MissionManager.CheckAIGoal(GoalType.DestroyUnit);
                temp_goal.mType = GoalType.DestroyUnit;
            }
            else
            {
                priority = MissionManager.CheckAIGoal(GoalType.AttackUnit);
                temp_goal.mType = GoalType.AttackUnit;
            }

            temp_goal.mScore = (priority + modifier) / distance;
            PossibleGoals.Add(new Goal(temp_goal));
            GameLog.RecordAI("Attack " + GameManager.mSingleton.otherPlayer.unit_mgr.all_units[j].name +
            ": " + temp_goal.mScore.ToString() + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());
        }

        //-----------------
        //ATTACK STRUCTURES

        //for each enemy structure, calculate the score
        for (int j = 0; j < GameManager.mSingleton.otherPlayer.unit_mgr.all_structures.Count; j++)
        {
            int enemy_hex = GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[j].getHexes()[0];
            distance = MapManager.findRange(pos, enemy_hex);
            modifier = GetAttackRating(GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[j].myCard);

            //find the CLOSEST hex
            for (int k = 1; k < GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[j].getHexes().Count; k++)
            {
                enemy_hex = GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[j].getHexes()[k];
                int temp_dist = MapManager.findRange(pos, enemy_hex);
                if (temp_dist < distance) distance = temp_dist;
            }

            temp_goal.mOther = j;

            //determine if this attack will destroy the other structure or not
            if (GameManager.mSingleton.WillTargetDie(GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[j], GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit]))
            {
                priority = MissionManager.CheckAIGoal(GoalType.DestroyStructure);
                temp_goal.mType = GoalType.DestroyStructure;
            }
            else
            {
                priority = MissionManager.CheckAIGoal(GoalType.AttackStructure);
                temp_goal.mType = GoalType.AttackStructure;
            }

            temp_goal.mScore = (priority + modifier) / distance;
            PossibleGoals.Add(new Goal(temp_goal));
            GameLog.RecordAI("Attack " + GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[j].name +
            ": " + temp_goal.mScore.ToString() + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());
        }

        //-----------------
        //ESCAPE COMBAT

        //for each enemy unit, calculate the score
        for (int j = 0; j < GameManager.mSingleton.otherPlayer.unit_mgr.all_units.Count; j++)
        {
            int enemy_hex = GameManager.mSingleton.otherPlayer.unit_mgr.all_units[j].getHex();
            distance = MapManager.findRange(pos, enemy_hex);
            modifier = GameManager.mSingleton.otherPlayer.unit_mgr.all_units[j].myCard.mEscapeRating;

            temp_goal.mOther = j;

            //determine if the enemy can destroy us
            if (GameManager.mSingleton.WillTargetDie(GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit], GameManager.mSingleton.otherPlayer.unit_mgr.all_units[j]))
            {
                priority = MissionManager.CheckAIGoal(GoalType.Escape);
                temp_goal.mType = GoalType.Escape;
            }
            else
            {
                priority = MissionManager.CheckAIGoal(GoalType.None);
                temp_goal.mType = GoalType.None;
            }

            temp_goal.mScore = (priority + modifier) / distance;
            PossibleGoals.Add(new Goal(temp_goal));
            GameLog.RecordAI("Escape " + GameManager.mSingleton.otherPlayer.unit_mgr.all_units[j].name +
            ": " + temp_goal.mScore + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());
        }

        //-----------------
        //USE ABILITIES

        //check this unit's map commands
        for (int j = 0; j < GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mapCommands.Count; j++)
        {
            if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mapCommands[j] == "Move" ||
                GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mapCommands[j] == "Attack" ||
                GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mapCommands[j] == "End Turn" ||
                GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mapCommands[j] == "Defend")
                continue;

            priority = MissionManager.CheckAIGoal(GoalType.Ability);
            temp_goal.mType = GoalType.Ability;

            //Get the ability we have selected
            int index = GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.GetMapAbilityIndex(GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mapCommands[j]);

            if (index < 0) continue;
            //Get the targets for this ability
            List<TargetableObject> Targets = new List<TargetableObject>();
            //for (int k = 0; k < GameManager.mSingleton.turnPlayer.unit_mgr.all_units[i].myCard.mAbilities[index].mAbilityList.Count; k++) //look through all chunks
            //{
            //INSTEAD of looking through all chunks, I am going to only look at the first chunk
            //we want to look at chunks with a set number of targets or blanket target, but not self targets or no targets
            if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].mAbilityList[0].mNumTargets != 0)
            {
                GameLog.Write(AIManager.curUnit.ToString() + "\t" + GameManager.mSingleton.turnPlayer.unit_mgr.all_units.Count.ToString() + "\t" +
                   GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.name + "\t");
                try
                {
                    if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].mAbilityList[0].mOwnerAdjacent)
                        Targets.AddRange(AbilityHandler.CheckAdjacentTargets(GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].mAbilityList[0]));
                    else
                        Targets.AddRange(AbilityHandler.CheckAutomaticTargets(GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].mAbilityList[0]));
                    AbilityHandler.mActivateAbility = false; //reset this, we are just checking targets for goals here
                }
                catch (Exception e) { Debug.Log("EXCEPTION: " + e.ToString()); continue; }
            }
            //}

            //Now we move on to calculate the score for this goal
            priority = MissionManager.CheckAIGoal(GoalType.Ability);
            temp_goal.mType = GoalType.Ability;
            temp_goal.mOther = index; //set the ability to use

            if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].mAbilityList[0].mNumTargets > 0)
            {
                //Check each target
                for (int k = 0; k < Targets.Count; k++)
                {
                    Card TargetCard = AbilityHandler.GetTargetCard(Targets[k]);

                    //skip this target if it shares our hex (because it is us)
                    if (TargetCard.mCurHexes.Contains(pos))
                        continue;

                    int enemy_hex = TargetCard.mCurHexes[0];
                    distance = MapManager.findRange(pos, enemy_hex);
                    //find the CLOSEST hex
                    for (int x = 1; x < TargetCard.mCurHexes.Count; x++)
                    {
                        enemy_hex = TargetCard.mCurHexes[x];
                        int temp_dist = MapManager.findRange(pos, enemy_hex);
                        if (temp_dist < distance) distance = temp_dist;
                    }

                    //Get the modifier for this ability
                    modifier = GetAbilityModifier(TargetCard, GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index]);
                    temp_goal.mScore = (priority + modifier) / distance;
                    temp_goal.mCard = TargetCard;

                    if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].CanBeExecuted(GameManager.mSingleton.turnPlayer, GameManager.mSingleton.otherPlayer))
                        PossibleGoals.Add(new Goal(temp_goal));

                    GameLog.RecordAI("Use " + GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].mDisplayName + " on " + TargetCard.name +
                    ": " + temp_goal.mScore + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());
                }
            }
            else if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].mAbilityList[0].mNumTargets < 0)
            {
                // only set one possible goal, increasing the modifier per target we have (b/c the ability will be more effective)
                distance = 10; //with a blanket target, set a default distance
                modifier = GetAbilityModifier(Targets, GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index]);
                temp_goal.mScore = (priority + modifier) / distance;
                temp_goal.mCard = new Card();

                if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].CanBeExecuted(GameManager.mSingleton.turnPlayer, GameManager.mSingleton.otherPlayer))
                    PossibleGoals.Add(new Goal(temp_goal));

                GameLog.RecordAI("Use " + GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].mDisplayName +
                ": " + temp_goal.mScore + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());
            }
            else //num targets == 0
            {
                //the ability is targeting himself and we can ignore targets, and can check the tag to see stuff
                distance = 10; //with a blanket target, set a default distance
                modifier = GetAbilityModifier(GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index]);
                temp_goal.mScore = (priority + modifier) / distance;
                temp_goal.mCard = new Card();

                if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].CanBeExecuted(GameManager.mSingleton.turnPlayer, GameManager.mSingleton.otherPlayer))
                    PossibleGoals.Add(new Goal(temp_goal));

                GameLog.RecordAI("Use " + GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.mAbilities[index].mDisplayName +
                ": " + temp_goal.mScore + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Distance: " + distance.ToString());
            }

        }

        //-----------------
        //GET OPTIMAL GOAL

        AssignedGoals.Add(GetOptimalGoal(true, false));
        GameLog.RecordAI(GameManager.mSingleton.turnPlayer.unit_mgr.all_units[AIManager.curUnit].myCard.name + " has a goal of " + AssignedGoals.Last().mType.ToString());


        //for every structure, try to use abilities or attack (no movement necessary)

    } //end of AssignGoals()

    /// <summary>
    /// Return the highest scoring goal
    /// </summary>
    /// <param name="clear">Decide whether to clear the list or not</param>
    /// /// <param name="remove">Decide whether to remove the found goal</param>
    /// <returns></returns>
    public static Goal GetOptimalGoal(bool clear, bool remove)
    {
        Goal optimal = new Goal();
        int index = 0;
        for (int i = 0; i < PossibleGoals.Count; i++)
        {

            if (PossibleGoals[i].mScore > optimal.mScore)
            {
                optimal = PossibleGoals[i];
                index = i;
            }
        }

        if (remove)
            PossibleGoals.RemoveAt(index);
        else if (clear)
            PossibleGoals.Clear();

        return optimal;
    }

    /// <summary>
    /// Return the best unit/structure for the situation.
    /// </summary>
    /// <returns></returns>
    public static Goal GetOptimalFieldObject()
    {
        Goal optimal = new Goal();

        int pStatFocus = GetBestStatFocus();
        switch (pStatFocus)
        {
            case -1: //no particular focus
                return GetOptimalCard();
            case 0: //hp/range focus
                float pMaxHp = 0;
                float pRange = 0; // used as tiebreaker

                // finds card with highest health
                for (int i = 0; i < PossibleGoals.Count; i++)
                {
                    if (PossibleGoals[i].mCard.getHealth() > pMaxHp)
                    {
                        optimal = PossibleGoals[i];
                        pMaxHp = PossibleGoals[i].mCard.getHealth();
                        pRange = PossibleGoals[i].mCard.getRangeMax();
                    }
                    else if (PossibleGoals[i].mCard.getHealth() == pMaxHp && PossibleGoals[i].mCard.getRangeMax() > pRange)
                    {
                        optimal = PossibleGoals[i];
                        pMaxHp = PossibleGoals[i].mCard.getHealth();
                        pRange = PossibleGoals[i].mCard.getRangeMax();
                    }
                }
                break;
            case 1: // highest movement/attack set
                float pMove = 0;
                float pAttack = 0; // used as tiebreaker

                // finds card with highest health
                for (int i = 0; i < PossibleGoals.Count; i++)
                {
                    if (PossibleGoals[i].mCard.getMovement() > pMove)
                    {
                        optimal = PossibleGoals[i];
                        pMove = PossibleGoals[i].mCard.getMovement();
                        pAttack = PossibleGoals[i].mCard.getAttack();
                    }
                    else if (PossibleGoals[i].mCard.getMovement() == pMove && PossibleGoals[i].mCard.getAttack() > pAttack)
                    {
                        optimal = PossibleGoals[i];
                        pMove = PossibleGoals[i].mCard.getMovement();
                        pAttack = PossibleGoals[i].mCard.getAttack();
                    }
                }
                break;
            case 2: // structures with best HP/cost
                float pHpPerCost = 0;
 
                // finds structure with highest health
                for (int i = 0; i < PossibleGoals.Count; i++)
                {
                    Card pCard = PossibleGoals[i].mCard;
                    if (pCard.getHealth() / pCard.getCost() > pHpPerCost && (pCard.getType() == 's' || pCard.getType() == 'n'))
                    {
                        optimal = PossibleGoals[i];
                        pHpPerCost = PossibleGoals[i].mCard.getHealth() / PossibleGoals[i].mCard.getCost();
                    }
                }
                //get another optimal card if we found nothing
                if (optimal.mCard.name == "Default")
                {
                    return GetOptimalCard();
                }
                break;
        }

        return optimal;
    }

    /// <summary>
    /// Looks at field, tries to find the best stat to focus on. 
    /// </summary>
    /// <returns>-1 for no stat, 0 for HP, 1 for move, 2 for range, 3 for structures</returns>
    public static int GetBestStatFocus()
    {
        // assumes list is already built thanks to CheckForUnitPlay(), so doesn't rebuild list
        CheckForUnitPlay(); 

        // offensive variables to check against
        float pAttackThreshold = 15, pRangeThreshold = 3, pMoveThreshold = 6;
        float pAttackCalc = 0, pRangeCalc = 0, pMoveCalc = 0;
        foreach (Unit pUnit in mNearbyEnemyUnits)
        {
            // tries to find the highest attack value that passes threshold
            if (pUnit.getAttack() > pAttackThreshold)
            {
                if (pUnit.getAttack() - pAttackThreshold > pAttackCalc)
                    pAttackCalc = (pUnit.getAttack() - pAttackThreshold) / pAttackThreshold;
            }
            // tries to find the highest range value that passes threshold
            if (pUnit.getRangeMax() > pRangeThreshold)
            {
                if (pUnit.getRangeMax() - pRangeThreshold > pRangeCalc)
                    pRangeCalc = (pUnit.getRangeMax() - pRangeThreshold) / pRangeThreshold;
            }
            // tries to find the highest move value that passes threshold
            if (pUnit.getMovement() > pMoveThreshold)
            {
                if (pUnit.getMovement() - pMoveThreshold > pMoveCalc)
                    pMoveCalc = (pUnit.getMovement() - pMoveThreshold) / pMoveThreshold;
            }
        }

        // defensive variables to check against - none for now?
        if (pAttackCalc > pRangeCalc && pAttackCalc > pMoveCalc) return 0;  // focus on HP to counter attack
        else if (pRangeCalc > pAttackCalc && pRangeCalc > pMoveCalc) return 1; // focus on move to counter range
        else if (pMoveCalc > pAttackCalc && pMoveCalc > pRangeCalc) return 2; // focus on structs to counter move

        return -1; // none in particular
    }

    /// <summary>
    /// Return the highest scoring card
    /// </summary>
    /// <returns></returns>
    public static Goal GetOptimalCard()
    {
        Goal optimal = new Goal();
        int index = 0;
        for (int i = 0; i < PossibleGoals.Count; i++)
        {
            if (PossibleGoals[i].mScore > optimal.mScore)
            {
                optimal = PossibleGoals[i];
                index = i;
            }
        }

        return optimal;
    }

    /// <summary>
    /// Checks for whether AI wants to play a unit
    /// </summary>
    /// <returns>True if unit should be played, false otherwise</returns>
    public static bool CheckForUnitPlay() {
    //    GameLog.Write("Check should we play unit");
        int pCheckDistance = 999;
        FindNearbyObjects(MapManager.P1_Base, pCheckDistance); // checks entire map, change later?

        bool pIsEnemyHeavy = false;
        float pUnitOffset = CalculateUnitOffset(ref pIsEnemyHeavy);

        //  GameLog.Write("Is enemy heavy? " + pIsEnemyHeavy.ToString() + " and unit offset? " + pUnitOffset.ToString());

        if (pIsEnemyHeavy || pUnitOffset <= MissionManager.CheckAIUnitOffset())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the lowest scoring goal
    /// </summary>
    /// <param name="clear">Decide whether to clear the list or not</param>
    /// <param name="remove">Decide whether to remove the found goal</param>
    /// <returns></returns>
    public static Goal GetLeastOptimalGoal(bool clear, bool remove)
    {
        Goal optimal = new Goal();
        int index = 0;
        optimal.mScore = 1000;

        for (int i = 0; i < PossibleGoals.Count; i++)
        {
            if (PossibleGoals[i].mScore < optimal.mScore)
            {
                optimal = PossibleGoals[i];
                index = i;
            }
        }

        if (remove)
            PossibleGoals.RemoveAt(index);
        else if (clear)
            PossibleGoals.Clear();

        return optimal;
    }

    /// <summary>
    /// returns the the move target of a given Goal
    /// </summary>
    /// <returns></returns>
   // public static int GetMoveTarget(Goal goal, int curHex, List<int> hexes, bool flying, int movesLeft)
    public static int GetMoveTarget(Goal goal, Unit unit, List<int> hexes)
    {
        int final_goal = 0;
        int move_target = 0;

        GameLog.Write(unit.myCard.name + ": Goal is " + goal.mType.ToString());

        switch (goal.mType)
        {
            case GoalType.AttackBase: //return enemy base hex
                if (GameManager.mSingleton.turnPlayer.isP1()) final_goal = MapManager.P2_Base;
                else final_goal = MapManager.P1_Base;
                break;
            case GoalType.ProtectBase:
            case GoalType.AttackUnit:
            case GoalType.DestroyUnit: //return mOther unit hex
                final_goal = GameManager.mSingleton.otherPlayer.unit_mgr.all_units[goal.mOther].getHex();
                break;
            case GoalType.AttackStructure:
            case GoalType.DestroyStructure: //return mOther structure hex
                final_goal = GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[goal.mOther].getHexes()[0];
                int distance = MapManager.findRange(unit.curHex, final_goal);
                //pathfind to the CLOSEST hex, not the default hex
                for (int k = 1; k < GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[goal.mOther].getHexes().Count; k++)
                {
                    int possible_goal = GameManager.mSingleton.otherPlayer.unit_mgr.all_structures[goal.mOther].getHexes()[k];
                    int temp_dist = MapManager.findRange(unit.curHex, possible_goal);
                    if (temp_dist < distance)
                    {
                        distance = temp_dist;
                        final_goal = possible_goal;
                    }
                }
                break;
            case GoalType.EarlyCapture:
            case GoalType.CaptureSpire: //retrun mOther spire index
                final_goal = MapManager.CAPTURE_HEXES[goal.mOther].hex;
                break;
            case GoalType.Escape: //return hex away from enemies?
                final_goal = MapManager.getRandomMoveLocation(hexes); //TEMP, figure out something better
                break;
            case GoalType.None: //return random hex
                final_goal = MapManager.getRandomMoveLocation(hexes);
                break;
            case GoalType.Ability: //find the ability target
                if (goal.mCard.name != "Default")
                {
                    final_goal = goal.mCard.mCurHexes[0];
                    int dist = MapManager.findRange(unit.curHex, final_goal);
                    //find the CLOSEST hex
                    for (int x = 1; x < goal.mCard.mCurHexes.Count; x++)
                    {
                        int possible_goal = goal.mCard.mCurHexes[x];
                        int temp_dist = MapManager.findRange(unit.curHex, possible_goal);
                        if (temp_dist < dist)
                        {
                            dist = temp_dist;
                            final_goal = possible_goal;
                        }
                    }
                }
                else
                {
                    //get a random move hex
                    goto case GoalType.None;
                }
                //final_goal = GameManager.mSingleton.turnPlayer.unit_mgr.all_units[goal.mOther].getHex();
                //could be gunning for a structure, or it might not even matter
                break;
            default:
                return -1;
        }


        //TARGET CHOICE 1: GOAL IS COMBAT, SO WE WANT TO GET THE PROPER MOVEMENT HEX
        if (isGoalTypeCombat(goal.mType) && MapManager.InTotalAttackRange(unit, unit.myCard.is_P1, final_goal))
        {
            //move_target = GameManager.mSingleton.GetAIAttackLocation(unit.curHex, final_goal);
            move_target = GameManager.mSingleton.GetAttackOriginHex(unit.curHex, final_goal); 
        }
        //OTHERWISE, JUST FOLLOW THE PATH AS NORMAL
        else
        {
            List<int> path = MapManager.getMovementPath(unit.curHex, final_goal, GameManager.mSingleton.turnPlayer.isP1(), false, unit.myCard.flying);

            //   GameLog.Write("***Movement***");
            //    foreach (int z in hexes) { GameLog.Write("HEXES contains " + z.ToString()); }
            //    foreach (int z in path) {    GameLog.Write("PATH contains " + z.ToString());  }

            //Loop through the complete path to find the movement target for this turn
            int x = 0;
            for (int i = 0; i < path.Count; i++)
            {
                if (hexes.Contains(path[i]) || path[i] == unit.curHex) //this will overwrite as we get further along the path, stopping when we find the hex we want
                                                                       //make sure that our movement range contains this hex and no other unit has chosen to go there
                {
                    //if(!MoveTargets.Contains(path[i]))//second check goes here b/c if this hex is chosen I still ma
                    move_target = path[i];
                    x = i;
                }
                else if (unit.myCard.flying && MapManager.findRange(unit.curHex, path[i]) - 1 <= unit.movesLeft)
                {
                    move_target = path[i];
                    x = i;
                }
                else //if the list of move hexes does not contain path[i], then it is outside our move range and we have already found our best hex
                {
                 //   path.RemoveRange(i, path.Count - i);
                    break;
                }
            }

            //check to make sure that we CAN move to the chosen target
            //otherwise run the move target all the back to our hex if need be until we find a valid move target
            while (unit.myCard.flying && (MapManager.isOccupiedAll(move_target) || MapManager.isCaptureHex(move_target) >= 0 || MapManager.isWater(move_target)))
            {
                x--;
                if (x < 0) {  move_target = unit.curHex; break; }
                move_target = path[x];
            }

        }

        MoveTargets.Add(move_target);
        return move_target;
    }

    /// <summary>
    /// Dynamically calculates card's attack rating based on stats/abilities. 
    /// </summary>
    /// <param name="pCard"></param>
    /// <returns></returns>
    public static int GetAttackRating(Card pCard) {
        // base number
        float pRating = 0;

        // boost if target is commander
        if (pCard.type == 'c') {
            pRating += 100; 
        }

        // calculate stats here
        pRating += pCard.getCurAttack(); 
        pRating += pCard.getCurRangeMax() * 5; 

        // calculate abilities
        foreach (AbilityBlock pAbility in pCard.mAbilities) {
            if (pAbility.mTimesLeft == 0) continue; // skip the ability if no longer usable

            // check chunks
            foreach (AbilityChunk pChunk in pAbility.mAbilityList) {
                switch (pChunk.mEffect) {
                // healing abilities
                case EffectType.HealthMod:
                    if (pChunk.mPrimaryValue < 0) {
                        pRating += pChunk.mPrimaryValue + 10;
                    } else {
                        // lowers threat if they're self-damaging
                        if (pChunk.mTargetType == TargetType.Self) pRating -= pChunk.mPrimaryValue; 
                        else pRating += pChunk.mPrimaryValue;
                    }
                    break;
                // ap mod abilities
                case EffectType.APMod:
                    pRating += 15 * pChunk.mPrimaryValue;
                    break;
                case EffectType.Summon: 
                    pRating += 30; 
                    break;
                // buffing/debuffing abilities
                case EffectType.StatMod:
                case EffectType.AttackMod:
                case EffectType.DefenceMod:
                case EffectType.RangeMaxMod:
                    int pMod = 10; 
                    if (pChunk.mPrimaryValue > 1) pRating += pMod; 
                    else pRating -= pMod; 
                    break;
                // higher boost on mult abilities
                case EffectType.StatMult:
                case EffectType.AttackMult:
                case EffectType.DefenceMult:
                case EffectType.RangeMaxMult:
                    pMod = 15;
                    if (pChunk.mPrimaryValue > 1) pRating += pMod;
                    else pRating -= pMod;
                    break;
                default:  // anything i haven't covered
                    break;
                }

                // prioritizes things that benefit specific side
                switch (pChunk.mTargetPlayer) {
                case TargetPlayer.AllAllies:
                    if (pChunk.mTargetType != TargetType.Self) pRating += 15;
                    break;
                case TargetPlayer.AllEnemies:
                    pRating += 10; 
                    break;
                }
            }
        }

        return (int)pRating;
    }

    public static int GetCardScore(Card card)
    {
        int score = 0;
        int priority = 0;
        int modifier = 0;
        int count = 0;

        //get the number of existing units/structures using this ta
        for (int i = 0; i < GameManager.mSingleton.turnPlayer.unit_mgr.all_units.Count; i++) {
            if (GameManager.mSingleton.turnPlayer.unit_mgr.all_units[i].myCard.mTag == card.mTag)
                count++;
        }
        for (int i = 0; i < GameManager.mSingleton.turnPlayer.unit_mgr.all_structures.Count; i++) {
            if (GameManager.mSingleton.turnPlayer.unit_mgr.all_structures[i].myCard.mTag == card.mTag)
                count++;
        }

        //now calculate the score: Add the priority first
        priority = GameManager.mSingleton.turnPlayer.Priorities[(int)card.mTag];
        //adjust it based on the type of card
        switch (card.type)
        {
            case 'c': if (GameManager.mSingleton.turnPlayer.notReachedLimit()) modifier = GameManager.mSingleton.turnPlayer.Priorities[(int)CardTag.Commander]; break;
            case 'u': if (GameManager.mSingleton.turnPlayer.notReachedLimit()) modifier = GameManager.mSingleton.turnPlayer.Priorities[(int)CardTag.Unit]; break;
            case 's': modifier = GameManager.mSingleton.turnPlayer.Priorities[(int)CardTag.Structure]; break;
            case 'i': modifier = GameManager.mSingleton.turnPlayer.Priorities[(int)CardTag.Instant]; break;
            case 'm': if (GameManager.mSingleton.turnPlayer.notReachedLimit()) modifier = GameManager.mSingleton.turnPlayer.Priorities[(int)CardTag.MinionUnit]; break;
            case 'n': modifier = GameManager.mSingleton.turnPlayer.Priorities[(int)CardTag.MinionStructure]; break;
        }
        //adjust the modifier by the chosen card's rating
        modifier += card.mRating;
        //and adjust it based on the number of existing identical tags
        count *= GameManager.mSingleton.turnPlayer.Priorities[(int)CardTag.PerCard];

        //calculate the final score
        score = priority + modifier + count;

        //Record the calcualations
        GameLog.RecordAI("Spawn scores for " + card.name);
        GameLog.RecordAI("Score: " + score.ToString() + " | Priority: " + priority.ToString() + " | Modifier: " + modifier.ToString() + " | Count: " + count.ToString());

        return score;
    }

    /// <summary>
    /// Calculates what the modifier should be for the ability given the ability tag
    /// </summary>
    public static int GetAbilityModifier(AbilityBlock ability)
    {
        int modifier = 0;
        //BeginSwitch:
        switch (ability.mOptimal)
        {
            case OptimalUse.Always:
                modifier = 50;
                break;
            case OptimalUse.Infrequent:
                modifier = 10;
                break;
            case OptimalUse.Frequent:
                modifier = 30;
                break;
            case OptimalUse.None:
                //do nothing
                break;
            case OptimalUse.Never:
                modifier = -100;
                break;
            case OptimalUse.EnemyZone:
                if ((ability.mHolder.is_P1 && MapManager.isP2Zone(ability.mHolder.mCurHexes[0]))
                    || (!ability.mHolder.is_P1 && MapManager.isP1Zone(ability.mHolder.mCurHexes[0])))
                    modifier = 30;
                //else do nothing
                break;
        }

        return modifier;
    }
    /// <summary>
    /// Overload that checks the stats of a single target
    /// </summary>
    public static int GetAbilityModifier(Card target, AbilityBlock ability)
    {
        int modifier = 0;
    BeginSwitch:
        switch (ability.mOptimal)
        {
            case OptimalUse.Always:
                modifier = 50;
                break;
            case OptimalUse.Infrequent:
                modifier = 10;
                break;
            case OptimalUse.Frequent:
                modifier = 30;
                break;
            case OptimalUse.None:
                //do nothing
                break;
            case OptimalUse.EnemyZone:
                if ((ability.mHolder.is_P1 && MapManager.isP2Zone(ability.mHolder.mCurHexes[0]))
                    || (!ability.mHolder.is_P1 && MapManager.isP1Zone(ability.mHolder.mCurHexes[0])))
                    modifier = 30;
                //else do nothing
                break;
            case OptimalUse.HighHealth:
                modifier = target.healthLeft;
                break;
            case OptimalUse.HighMovement:
                modifier = (target.curMovement * 3);
                break;
            case OptimalUse.HighAttack:
                modifier = target.curAttack;
                break;
            case OptimalUse.LowHealth:
                modifier = 30 - target.healthLeft;
                break;
            case OptimalUse.LowAttack:
                modifier = 30 - target.curAttack;
                break;
            case OptimalUse.Never:
                modifier = -100;
                break;
            case OptimalUse.Rune:
                ability.mOptimal = GameManager.mSingleton.turnPlayer.deck.Rune.mAbilities[0].mOptimal;
                goto BeginSwitch;
            case OptimalUse.HasAbility:
                if (target.mAbilities.Count > 0)
                    modifier = 30;
                //else do nothing
                break;
        }

        modifier /= 5; //need to lower the values on this

        return modifier;
    }
    /// <summary>
    /// Overload that checks the stats of multiple targets
    /// </summary>
    public static int GetAbilityModifier(List<TargetableObject> targets, AbilityBlock ability)
    {
        int modifier = 0;
        int count = 0;
    BeginSwitch:
        switch (ability.mOptimal)
        {
            case OptimalUse.Always:
                modifier = 50;
                break;
            case OptimalUse.Infrequent:
                modifier = 10;
                break;
            case OptimalUse.Frequent:
                modifier = 30;
                break;
            case OptimalUse.None:
                //do nothing
                break;
            case OptimalUse.EnemyZone:
                if ((ability.mHolder.is_P1 && MapManager.isP2Zone(ability.mHolder.mCurHexes[0]))
                   || (!ability.mHolder.is_P1 && MapManager.isP1Zone(ability.mHolder.mCurHexes[0])))
                    modifier = 30;
                //else do nothing
                break;
            case OptimalUse.MoreEnemies:
                foreach (TargetableObject t in targets)
                {
                    Card temp = AbilityHandler.GetTargetCard(t);
                    if ((temp.is_P1 && MapManager.isP2Zone(ability.mHolder.mCurHexes[0]))
                    || (!temp.is_P1 && MapManager.isP1Zone(ability.mHolder.mCurHexes[0])))
                        count++;
                }
                modifier = (count * 3);
                break;
            case OptimalUse.MoreTargets:
                modifier = (targets.Count * 3);
                break;
            case OptimalUse.HighHealth:
                foreach (TargetableObject t in targets)
                {
                    Card temp = AbilityHandler.GetTargetCard(t);
                    if (temp.healthLeft > 25)
                        count++;
                }
                modifier = (count * 3);
                break;
            case OptimalUse.HighMovement:
                foreach (TargetableObject t in targets)
                {
                    Card temp = AbilityHandler.GetTargetCard(t);
                    if (temp.curMovement > 5)
                        count++;
                }
                modifier = (count * 3);
                break;
            case OptimalUse.HighAttack:
                foreach (TargetableObject t in targets)
                {
                    Card temp = AbilityHandler.GetTargetCard(t);
                    if (temp.curAttack > 25)
                        count++;
                }
                modifier = (count * 3);
                break;
            case OptimalUse.LowHealth:
                foreach (TargetableObject t in targets)
                {
                    Card temp = AbilityHandler.GetTargetCard(t);
                    if (temp.healthLeft < 15)
                        count++;
                }
                modifier = (count * 3);
                break;
            case OptimalUse.LowAttack:
                foreach (TargetableObject t in targets)
                {
                    Card temp = AbilityHandler.GetTargetCard(t);
                    if (temp.curAttack < 15)
                        count++;
                }
                modifier = (count * 3);
                break;
            case OptimalUse.Rune:
                ability.mOptimal = GameManager.mSingleton.turnPlayer.deck.Rune.mAbilities[0].mOptimal;
                goto BeginSwitch;
        }

        return modifier;
    }

    static bool isGoalTypeCombat(GoalType goal)
    {
        if (goal == GoalType.AttackBase) return true;
        else if (goal == GoalType.AttackStructure) return true;
        else if (goal == GoalType.AttackUnit) return true;
        else if (goal == GoalType.DestroyStructure) return true;
        else if (goal == GoalType.DestroyUnit) return true;
        else return false;
    }

}