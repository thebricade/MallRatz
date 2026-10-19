namespace MallRatz.Players
{
    public enum PlayerRole { Spender, Friend }

    // Each Friend role can be taken by only one Friend (see Modes on the wiki).
    public enum FriendRole { None, Bouncer, Lookout, Returns, VoiceOfReason }
}
