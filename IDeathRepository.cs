namespace TerrariaDeathCounter
{
    interface IDeathRepository
    {
        int RecordDeath(string playerName, string killerName);
        string GetRecord(string playerName);
        int GetNumberOfDeaths(string playerName, string killerName);
        void ClearDeath();
    }
}
