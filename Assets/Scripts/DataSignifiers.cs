using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class DataSignifiers
{
    public const int AccountSignup = 1;
    public const int AccountSignin = 2;
    public const int Message = 3;
    public const int ServerLoginResponse = 4;
    public const int GameID = 5;
    public const int ServerGameIDResponse = 6;
    public const int BackOut = 7;
    public const int ServerGameRoomKick = 8;
    public const int ServerSendToLookingForPlayer = 9;
    public const int MessageToOpponent = 10;
    public const int SelectionToOpponent = 11;
    public const int AllSelectionsToObserver = 12;
    public const int AllSelectionsToObserverFinal = 13;
}

public enum GameStates { Login, EnterGameID, LookingForPlayer, PlayerMove, OpponentMove, Observer, Win, Lose, Draw, Finish };
