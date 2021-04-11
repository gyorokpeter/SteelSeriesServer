namespace SteelSeriesServer
{
    public abstract class Sender
    {
        public const int keyboardWidth = 21;   //must be at least 21

        abstract public void Start();
        abstract public void Stop();
        abstract public void SetGameName(string game);
        abstract public void SetColor(int key, byte r, byte g, byte b);
        abstract public void ApplyChanges();
    }
}