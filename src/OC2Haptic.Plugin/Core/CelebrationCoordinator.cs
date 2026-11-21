namespace OC2Haptic.Core
{
    public static class CelebrationCoordinator
    {
        public static GameEventType? Select(GameEventType? delivery,int star)
        {return star>=1 && star<=4 ? (GameEventType?)((GameEventType)((int)GameEventType.StarMilestone1+star-1)) : delivery;}
    }
}
