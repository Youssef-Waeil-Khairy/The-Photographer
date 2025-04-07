namespace SAE_Dubai.Leonardo.CameraSys
{
    [System.Serializable]
    public class CapturedPhoto
    {
        public System.DateTime TimeStamp;
        public int iso;
        public float aperture;
        public float shutterSpeed;
        public float focalLength;
        public float quality;
    }
}