namespace Scratch
{
    public class ScratchClass
    {
        static public string Serialize(ScratchClass sc) { return string.Empty; }

        void Do()
        {
            ScratchClass sc = new ScratchClass();
            string json = Scratch.ScratchClass.Serialize(sc);
        }
    }
}