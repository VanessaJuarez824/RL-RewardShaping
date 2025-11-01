public interface IRewardCalculator
{
    float CalculateReward(Coordenadas current, Coordenadas previous, bool hasKey, 
                         Coordenadas keyPos, Coordenadas goalPos, int currentEpisode);
    string GetModeName();
}