using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public static class NetworkingHandler {
    /// <summary>
    /// For playing a unit with networking.
    /// </summary>
    /// <param name="pManager"></param>
    /// <param name="pPlayCommand"></param>
    public static void NetworkPlayUnit(this GameManager pManager, FieldPlayCommand pPlayCommand) {
        int pHex = pPlayCommand.mHex;

        // reverses the hex for nonactive player
        //Debug.Log((pManager.turnPlayer.isP1() == pManager.P1) + "\t" + pHex + "\t" + (MapManager.MAP1.Length - pHex));
        //if (!pManager.PlayerOne.isP1()) pHex = MapManager.MAP1.Length - pHex;

        // figure out spawning player
        Player pSpawning, pEnemy;
        if (pPlayCommand.mIsTurnSpawning)
        {
            pSpawning = pManager.turnPlayer;
            pEnemy = pManager.otherPlayer;
        }
        else {
            pSpawning = pManager.otherPlayer;
            pEnemy = pManager.turnPlayer;
        }

        // figure out card
        Card pCard = new Card();
        Debug.Log("Play location is: " + pPlayCommand.mLocation.ToString());
        try
        {
            switch (pPlayCommand.mLocation)
            {
                case TargetLocation.Hand:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.Hand[pPlayCommand.mHandIndex];
                    else pCard = pEnemy.deck.Hand[pPlayCommand.mHandIndex]; break;
                case TargetLocation.SecondHand:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.SecondHand[pPlayCommand.mHandIndex];
                    else pCard = pEnemy.deck.SecondHand[pPlayCommand.mHandIndex]; break;
                case TargetLocation.Commander:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.Commander;
                    else pCard = pEnemy.deck.Commander; break;
                case TargetLocation.Discard:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.Discards[pPlayCommand.mHandIndex];
                    else pCard = pEnemy.deck.Discards[pPlayCommand.mHandIndex]; break;
                case TargetLocation.Deck:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.myDeck[pPlayCommand.mHandIndex];
                    else pCard = pEnemy.deck.myDeck[pPlayCommand.mHandIndex]; break;
                default:
                    Debug.Log("Card error");
                    break;
            }
        }
        catch(System.Exception e) { GameLog.Write("NetworkPlayUnit exception: " + e.ToString()); return; }

        //if we grabbed the wrong card, then create it
        if (pCard.identifier != pPlayCommand.mCardIdentifier)
        {
            pCard = new Card(pPlayCommand.mCardIdentifier, pPlayCommand.mCardType, pSpawning.isP1());
        }

        HUDManager.clickedCardLocation = pPlayCommand.mLocation;
        HUDManager.clickedCardPlayer = pPlayCommand.mCardPlayer;
        pManager.PlayUnit(ref pSpawning, ref pEnemy, pCard, pPlayCommand.mHandIndex, pHex, pPlayCommand.mCheckRules, pPlayCommand.mDontPayCost);
        AbilityHandler.SetupUpdate();
    }

    /// <summary>
    /// For playing a structure with networking.
    /// </summary>
    /// <param name="pManager"></param>
    /// <param name="pPlayCommand"></param>
    public static void NetworkPlayStructure(this GameManager pManager, FieldPlayCommand pPlayCommand) {
        int pHex = pPlayCommand.mHex;

        // reverses the hex for nonactive player
        //if (!pManager.PlayerOne.isP1()) pHex = MapManager.MAP1.Length - pHex;

        // figure out spawning player
        Player pSpawning, pEnemy;
        if (pPlayCommand.mIsTurnSpawning)
        {
            pSpawning = pManager.turnPlayer;
            pEnemy = pManager.otherPlayer;
        }
        else {
            pSpawning = pManager.otherPlayer;
            pEnemy = pManager.turnPlayer;
        }

        // figure out card
        Card pCard = new Card();
        try
        {
            switch (pPlayCommand.mLocation)
            {
                case TargetLocation.Hand:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.Hand[pPlayCommand.mHandIndex];
                    else pCard = pEnemy.deck.Hand[pPlayCommand.mHandIndex]; break;
                case TargetLocation.SecondHand:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.SecondHand[pPlayCommand.mHandIndex];
                    else pCard = pEnemy.deck.SecondHand[pPlayCommand.mHandIndex]; break;
                case TargetLocation.Commander:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.Commander;
                    else pCard = pEnemy.deck.Commander; break;
                case TargetLocation.Discard:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.Discards[pPlayCommand.mHandIndex];
                    else pCard = pEnemy.deck.Discards[pPlayCommand.mHandIndex]; break;
                case TargetLocation.Deck:
                    if (pPlayCommand.mCardPlayer) pCard = pSpawning.deck.myDeck[pPlayCommand.mHandIndex];
                    else pCard = pEnemy.deck.myDeck[pPlayCommand.mHandIndex]; break;
                default:
                    Debug.Log("Card error");
                    break;
            }
        }
        catch (System.Exception e) { GameLog.Write("NetworkPlayStructure exception: " + e.ToString()); return; }

        //if we grabbed the wrong card, then create it
        if (pCard.identifier != pPlayCommand.mCardIdentifier)
        {
            pCard = new Card(pPlayCommand.mCardIdentifier, pPlayCommand.mCardType, pSpawning.isP1());
        }

        HUDManager.clickedCardPlayer = pPlayCommand.mCardPlayer;
        HUDManager.clickedCardLocation = pPlayCommand.mLocation;
        pManager.PlayStructure(pCard, pPlayCommand.mHandIndex, ref pSpawning, ref pEnemy, pHex, pPlayCommand.mCheckRules, pPlayCommand.mDontPayCost);
        AbilityHandler.SetupUpdate();
    }

    public static void NetworkPlayInstant(this GameManager pManager, PlayInstantCommand pPlayCommand) {

        //set up clicked card info
        HUDManager.clickedCardLocation = pPlayCommand.mLocation;
        HUDManager.clickedCardPlayer = pPlayCommand.mCardPlayer;

        Debug.Log("Network play instant stats: [Index] " + pPlayCommand.mTarget.ToString() + "\t[Location] " +
            HUDManager.clickedCardLocation.ToString() + "\t[Player] " + pPlayCommand.mCardPlayer.ToString());

        if (pPlayCommand.mTarget >= 100) HUDManager.clickedCardLocation = TargetLocation.Rune; //set up our Rune to play

        pManager.PlayInstant(pPlayCommand.mTarget, !pPlayCommand.mDontPayCost);

        //playing spells already call an update, so this isn't entirely necessary
        //AbilityHandler.SetupUpdate();
    }

    public static void NetworkAddFieldTarget(this GameManager pManager, FieldTargetCommand pPlayCommand) {
        int pHex = pPlayCommand.mTargetHex;
        // reverses the hex for nonactive player
        //if (!pManager.PlayerOne.isP1()) pHex = MapManager.MAP1.Length - pHex;

        Debug.Log("About to select field effect target");
        AbilityHandler.SelectFieldEffectTarget((TargetType)pPlayCommand.mTarget, pHex);

        //reset the targeting state so abilities can resolve
        if (pManager.mCurrentState == GameState.Targeting && AbilityHandler.mAbilityTargets.Count >= pPlayCommand.mNumTargets)
        {
            pManager.mCurrentState = GameState.Normal;
            pManager.clearValidHexes();
        }
    }

    public static void NetworkAddNonFieldTarget(this GameManager pManager, NonFieldTargetCommand pPlayCommand) {
        int pPopupType = pPlayCommand.mPopupType, pSelectionNum = pPlayCommand.mSelectionNum;
        bool pIsTurnPlayer = pPlayCommand.mIsTurnPlayer;

        // this will be complicated
        List<Card> pTargetList = new List<Card>();

        switch ((PopupType)pPopupType)
        {
            case PopupType.P1_Discard: //P1 discards
            case PopupType.P2_Discard: //P2 discards
            case PopupType.Both_Discard:
                if (pIsTurnPlayer) { pTargetList = pManager.turnPlayer.deck.Discards; }
                else { pTargetList = pManager.otherPlayer.deck.Discards; }
                HUDManager.SetClickedCard(pSelectionNum, TargetLocation.Discard, pIsTurnPlayer);
                break;
            case PopupType.P1_Deck: //P1 deck
            case PopupType.P2_Deck: //P2 deck
            case PopupType.Both_Deck:
                if (pIsTurnPlayer) { pTargetList = pManager.turnPlayer.deck.myDeck; }
                else { pTargetList = pManager.otherPlayer.deck.myDeck; }
                break;
            case PopupType.P1_Hand: // hand
            case PopupType.P2_Hand:
            case PopupType.Both_Hand:
                if (pIsTurnPlayer) { pTargetList = pManager.turnPlayer.deck.Hand; }
                else { pTargetList = pManager.otherPlayer.deck.Hand; }
                break;
            case PopupType.P1_SecondHand: // secondary hand
            case PopupType.P2_SecondHand:
            case PopupType.Both_SecondHand:
                if (pIsTurnPlayer) { pTargetList = pManager.turnPlayer.deck.SecondHand; }
                else { pTargetList = pManager.otherPlayer.deck.SecondHand; }
                break;
            case PopupType.AllRunes:
                if (pIsTurnPlayer) { pTargetList = pManager.turnPlayer.deck.all_runes; }
                else { pTargetList = pManager.otherPlayer.deck.all_runes; }
                break;
        }

        // add correct target
        //GameLog.Write("Nonfield target number is " + pSelectionNum.ToString() + " and is targeting " + pPopupType.ToString() + " for turn player " + pIsTurnPlayer.ToString());
        //GameLog.Write("Adding nonfield target " + pTargetList[pSelectionNum].name);
        AbilityHandler.AddTarget(pTargetList[pSelectionNum]);
        if (AbilityHandler.pResolvingAbility.mRemoveCard) pTargetList.RemoveAt(pSelectionNum);

        //reset the targeting state so abilities can resolve
        if (pManager.mCurrentState == GameState.Targeting && AbilityHandler.mAbilityTargets.Count >= pPlayCommand.mNumTargets)
            pManager.mCurrentState = GameState.Normal;
    }

    public static void NetworkMoveUnit(this GameManager pManager, FieldTargetCommand pPlayCommand) {
        int pHex = pPlayCommand.mTargetHex;
        // reverses the hex for nonactive player
        //if (!pManager.PlayerOne.isP1()) pHex = MapManager.MAP1.Length - pHex;

        pManager.validHexes = MapManager.allMovementHexes(pManager.turnPlayer.unit_mgr.all_units[pPlayCommand.mTarget].getMovesLeft(),
                   pManager.turnPlayer.unit_mgr.all_units[pPlayCommand.mTarget].getHex(), pManager.turnPlayer.isP1(),
                   pManager.turnPlayer.unit_mgr.all_units[pPlayCommand.mTarget].myCard.ignoresZOC(),
                   pManager.turnPlayer.unit_mgr.all_units[pPlayCommand.mTarget].myCard.flying);
        pManager.pathHexes = MapManager.getMovementPath(pManager.turnPlayer.unit_mgr.all_units[pPlayCommand.mTarget].getHex(),
            pHex, pManager.turnPlayer.isP1(), true, pManager.turnPlayer.unit_mgr.all_units[pPlayCommand.mTarget].myCard.flying);

        pManager.turnPlayer.unit_mgr.setSelectedUnit(pPlayCommand.mTarget);

        //handle movestart abilities
        AbilityHandler.CheckTriggers(TriggerTime.MoveStart);
        AbilityHandler.CheckAutoTriggers(TriggerTime.MoveStart);

        pManager.HandleMovement(pPlayCommand.mTarget, pHex);

     //   if (!AbilityHandler.AlreadyUpdating())
        {
         //   Debug.Log("Updating from network move unit (NOT already updating)");
            AbilityHandler.SetupUpdate();
        }
    }

    public static void NetworkDiscardCard(this GameManager pManager, DiscardCommand pPlayCommand) {
        Player pTarget;
        if (pPlayCommand.mIsTurnPlayer) pTarget = pManager.turnPlayer;
        else pTarget = pManager.otherPlayer;

        pTarget.DiscardCard(pPlayCommand.mHandIndex);

        //reset the discarding state so abilities can resolve
        if (pManager.mCurrentState == GameState.Discarding && pPlayCommand.mCardsToDiscard <= 1)
            pManager.mCurrentState = GameState.Normal;
    }

    public static void NetworkXpUpgrade(this GameManager pManager, UpgradeCommand pUpgradeCommand) {
        Player pTarget;
        if (pUpgradeCommand.mIsPlayerOne) pTarget = pManager.PlayerOne;
        else pTarget = pManager.PlayerTwo;

        if (pUpgradeCommand.mIsBaseUpgrade) pTarget.upgrade_mgr.BasePurchaseInner(pUpgradeCommand.mPath, pUpgradeCommand.mTier, false);
        else pTarget.upgrade_mgr.MinionPurchaseInner(pUpgradeCommand.mPath, pUpgradeCommand.mTier, false);
        AbilityHandler.SetupUpdate();
    }

    public static void NetworkCaptureSpire(this GameManager pManager, CaptureCommand pCaptureCommand) {
        Player pPlayer, pEnemy;
        if (pCaptureCommand.mIsPlayerOne)
        {
            pPlayer = pManager.PlayerOne;
            pEnemy = pManager.PlayerTwo;
        }
        else
        {
            pPlayer = pManager.PlayerTwo;
            pEnemy = pManager.PlayerOne;
        }

        if (pCaptureCommand.mCapturing) pManager.CaptureSpireInner(pPlayer, pEnemy, pCaptureCommand.mCaptureIndex, true);
        else pManager.ContestSpireInner(pPlayer, pEnemy, pCaptureCommand.mCaptureIndex);
    }

    public static void NetworkExecuteAbility(this GameManager pManager, ExecuteCommand pExecuteCommand) {

        //Find the card using the location and execute the correct ability
        switch (pExecuteCommand.mLocation)
        {
            default:
            case TargetLocation.Hand:
                pManager.turnPlayer.deck.Hand[pExecuteCommand.mTarget].Execute(pExecuteCommand.mAbility, ref pManager.turnPlayer);
                break;
            case TargetLocation.SecondHand:
                pManager.turnPlayer.deck.SecondHand[pExecuteCommand.mTarget].Execute(pExecuteCommand.mAbility, ref pManager.turnPlayer);
                break;
            case TargetLocation.Commander:
                pManager.turnPlayer.deck.Commander.Execute(pExecuteCommand.mAbility, ref pManager.turnPlayer);
                break;
            case TargetLocation.Map:
                if (pExecuteCommand.mType == 'u' || pExecuteCommand.mType == 'm' || pExecuteCommand.mType == 'c')  //execute unit
                {
                    pManager.AutoFocus(pManager.turnPlayer.unit_mgr.all_units[pExecuteCommand.mTarget].myCard.mCurHexes[0], true);
                    pManager.turnPlayer.unit_mgr.all_units[pExecuteCommand.mTarget].myCard.Execute(pExecuteCommand.mAbility, ref pManager.turnPlayer);
                }
                else  // execute structure
                {
                    pManager.AutoFocus(pManager.turnPlayer.unit_mgr.all_structures[pExecuteCommand.mTarget].myCard.mCurHexes[0], true);
                    pManager.turnPlayer.unit_mgr.all_structures[pExecuteCommand.mTarget].myCard.Execute(pExecuteCommand.mAbility, ref pManager.turnPlayer);
                }
                break;
        }

    }


}