namespace SAE_Dubai.Leonardo.Client_System
{
    /// <summary>
    /// Defines different types of portrait composition based on framing distance.
    /// </summary>
    public enum PortraitShotType
    {
        Undefined = -1,
        ExtremeCloseUp, // ECU - Eye/Feature only.
        BigCloseUp, // BCU - Forehead to Chin.
        CloseUp, // CU  - Head to Neck/Shoulders.
        MediumCloseUp, // MCU - Head to Mid-Chest.
        MidShot, // MS  - Head to Waist/Hip.
        MediumLongShot, // MLS - Head to Knees (American Shot).
        LongShot, // LS  - Full Body, prominent.
        VeryLongShot, // VLS - Full Body, smaller in frame.
        ExtremeLongShot // XLS - Full Body, tiny in frame (Environment focus).
    }
}